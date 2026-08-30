using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: TUnit.DocSnippetGenerator <repository-root> <output-directory>");
    return 1;
}

var repositoryRoot = Path.GetFullPath(args[0]);
var outputDirectory = Path.GetFullPath(args[1]);
var documents = new[] { Path.Combine(repositoryRoot, "README.md") }
    .Concat(Directory.EnumerateFiles(Path.Combine(repositoryRoot, "docs", "docs"), "*.md", SearchOption.AllDirectories))
    .Concat(Directory.EnumerateFiles(Path.Combine(repositoryRoot, "docs", "docs"), "*.mdx", SearchOption.AllDirectories))
    .Order(StringComparer.Ordinal)
    .ToArray();

var snippets = new List<Snippet>();
var documentedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var document in documents)
{
    ReadDocument(document);
}

Directory.CreateDirectory(outputDirectory);
foreach (var oldFile in Directory.EnumerateFiles(outputDirectory, "Snippet*.g.cs"))
{
    File.Delete(oldFile);
}
var isolatedDirectory = Path.Combine(outputDirectory, "isolated");
if (Directory.Exists(isolatedDirectory))
{
    Directory.Delete(isolatedDirectory, recursive: true);
}

var isolatedSharedDocuments = snippets
    .Where(snippet => snippet.SharedDocumentId is not null && RequiresIsolatedCompilation(snippet.Prelude + snippet.Source))
    .Select(snippet => snippet.SharedDocumentId!)
    .ToHashSet(StringComparer.Ordinal);
var sharedSources = snippets
    .Where(snippet => snippet.SharedDocumentId is not null)
    .GroupBy(snippet => snippet.SharedDocumentId!, StringComparer.Ordinal)
    .ToDictionary(
        group => group.Key,
        group => string.Join('\n', group.Select(snippet => snippet.Source)),
        StringComparer.Ordinal);
var emittedAssemblyAttributes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
var isolatedSnippets = 0;
for (var index = 0; index < snippets.Count; index++)
{
    var snippet = snippets[index];
    var requiresIsolation = RequiresIsolatedCompilation(snippet.Prelude + snippet.Source) ||
                            snippet.SharedDocumentId is not null && isolatedSharedDocuments.Contains(snippet.SharedDocumentId);
    var isolatedGroupName = snippet.SharedDocumentId is null ? $"Snippet{index}" : $"Shared_{snippet.SharedDocumentId}";
    var snippetOutputDirectory = requiresIsolation
        ? Path.Combine(isolatedDirectory, isolatedGroupName)
        : outputDirectory;
    if (snippetOutputDirectory != outputDirectory)
    {
        isolatedSnippets++;
        Directory.CreateDirectory(snippetOutputDirectory);
    }

    if (!emittedAssemblyAttributes.TryGetValue(snippetOutputDirectory, out var assemblyAttributes))
    {
        assemblyAttributes = new HashSet<string>(StringComparer.Ordinal);
        emittedAssemblyAttributes[snippetOutputDirectory] = assemblyAttributes;
    }

    File.WriteAllText(
        Path.Combine(snippetOutputDirectory, $"Snippet{index}.g.cs"),
        GenerateSource(
            snippet,
            index,
            assemblyAttributes,
            snippet.SharedDocumentId is not null ? sharedSources[snippet.SharedDocumentId] : snippet.Source),
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

Console.WriteLine($"Generated {snippets.Count} C# documentation snippets ({isolatedSnippets} require isolated compilation). ");
Console.WriteLine($"Documented TUnit packages: {string.Join(", ", documentedPackages.Order())}");
return 0;

void ReadDocument(string documentPath)
{
    var relativePath = Path.GetRelativePath(repositoryRoot, documentPath).Replace('\\', '/');
    var lines = File.ReadAllLines(documentPath);
    var sharedDocumentId = lines.Any(line => line.Trim() == "<!-- doc-test-shared -->")
        ? Regex.Replace(relativePath, "[^A-Za-z0-9_]", "_")
        : null;
    ReadDocumentedPackages(lines);
    var maskingDirective = lines.FirstOrDefault(line =>
        line.Trim().StartsWith("<!-- doc-test-ignore", StringComparison.Ordinal) ||
        line.Trim().StartsWith("<!-- doc-test-contextual", StringComparison.Ordinal));
    if (maskingDirective is not null)
    {
        throw new InvalidOperationException(
            $"Failure-masking documentation directive is not supported in {relativePath}: {maskingDirective.Trim()}");
    }

    var csharpOrdinal = 0;
    var localSetups = new Dictionary<string, string>(StringComparer.Ordinal);

    for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
    {
        var csharpFence = Regex.Match(lines[lineIndex], "^(?<indent>[ \\t]*)```csharp\\s*$");
        if (csharpFence.Success)
        {
            csharpOrdinal++;
            var startLine = lineIndex + 2;
            var indentation = csharpFence.Groups["indent"].Value;
            var body = new List<string>();
            for (lineIndex++; lineIndex < lines.Length && !Regex.IsMatch(lines[lineIndex], "^\\s*```\\s*$"); lineIndex++)
            {
                body.Add(lines[lineIndex].StartsWith(indentation, StringComparison.Ordinal)
                    ? lines[lineIndex][indentation.Length..]
                    : lines[lineIndex]);
            }

            if (lineIndex >= lines.Length)
            {
                throw new InvalidOperationException($"Unclosed C# fence at {relativePath}:{startLine}.");
            }

            var rawSource = string.Join('\n', body);
            if (Regex.IsMatch(
                    rawSource,
                    @"(?im)^\s*#(?:pragma\s+warning\s+disable|nullable\s+disable|if\s+false)\b|\b(?:Unconditional)?SuppressMessage\b"))
            {
                throw new InvalidOperationException(
                    $"Failure-masking C# is not supported in {relativePath}:{startLine}.");
            }

            foreach (var (sectionSource, currentStartLine) in SplitFrameworkSections(rawSource, startLine))
            {
                var directive = currentStartLine == startLine && startLine >= 3
                    ? lines[startLine - 3].Trim()
                    : string.Empty;
                var source = sectionSource;
                source = Regex.Replace(source, "(?m)^\\s*#:[^\\r\\n]*$", string.Empty);
                source = Regex.Replace(source, "(?m)^([ \\t]*)(?:\\.\\.\\.|…)\\s*;?\\s*$", string.Empty);
                source = Regex.Replace(source, "\\{\\s*(?:\\.\\.\\.|…)\\s*\\}", "{ }");
                var sourceWithoutComments = Regex.Replace(source, "(?s)/\\*.*?\\*/", string.Empty);
                sourceWithoutComments = Regex.Replace(sourceWithoutComments, "(?m)//.*$", string.Empty);
                if (Regex.IsMatch(sourceWithoutComments, "(?:=>|=)\\s*(?:\\.\\.\\.|…)\\s*;"))
                {
                    throw new InvalidOperationException(
                        $"Incomplete C# at {relativePath}:{currentStartLine} is not compilable.");
                }

                var explicitMode = Regex.Match(directive, "^<!--\\s*doc-test-(declaration|member|statements)\\s*-->$");
                var splitMode = Regex.Match(directive, "^<!--\\s*doc-test-(declaration|member):\\s*split-before=(.+?)\\s*-->$");
                if (directive.StartsWith("<!-- doc-test-", StringComparison.Ordinal) &&
                    !explicitMode.Success &&
                    !splitMode.Success)
                {
                    throw new InvalidOperationException(
                        $"Unknown or malformed doc-test directive before {relativePath}:{currentStartLine}.");
                }

                var (usings, sourceWithoutUsings) = ExtractUsings(source);
                string? splitBefore = splitMode.Success ? splitMode.Groups[2].Value : null;
                SnippetMode mode;
                if (splitMode.Success)
                {
                    mode = Enum.Parse<SnippetMode>(splitMode.Groups[1].Value, ignoreCase: true);
                }
                else if (explicitMode.Success)
                {
                    mode = Enum.Parse<SnippetMode>(explicitMode.Groups[1].Value, ignoreCase: true);
                }
                else
                {
                    mode = ClassifyWithUsageSplit(sourceWithoutUsings, relativePath, csharpOrdinal, out splitBefore);
                }

                var prelude = string.Empty;
                if (mode == SnippetMode.Statements)
                {
                    var localDeclarations = GetLocalDeclarations(sourceWithoutUsings);
                    var declaredLocals = localDeclarations.Keys.ToHashSet(StringComparer.Ordinal);
                    var requiredSetups = ExpandRequiredSetups(sourceWithoutUsings, localSetups, declaredLocals);
                    prelude = string.Join('\n', requiredSetups);

                    foreach (var local in declaredLocals)
                    {
                        localSetups[local] = localDeclarations[local];
                    }
                }

                snippets.Add(new Snippet(
                    documentPath.Replace('\\', '/'),
                    sharedDocumentId,
                    currentStartLine,
                    usings,
                    prelude,
                    sourceWithoutUsings,
                    mode,
                    splitBefore));
            }

            continue;
        }
    }
}

static IReadOnlyList<string> ExpandRequiredSetups(
    string source,
    IReadOnlyDictionary<string, string> localSetups,
    ISet<string> declaredLocals)
{
    var result = new List<string>();
    var added = new HashSet<string>(StringComparer.Ordinal);
    var visiting = new HashSet<string>(StringComparer.Ordinal);

    void Add(string name)
    {
        if (declaredLocals.Contains(name) || added.Contains(name) || !localSetups.TryGetValue(name, out var declaration) || !visiting.Add(name))
        {
            return;
        }

        foreach (var dependency in localSetups.Keys)
        {
            if (dependency != name && Regex.IsMatch(declaration, $@"\b{Regex.Escape(dependency)}\b"))
            {
                Add(dependency);
            }
        }

        visiting.Remove(name);
        added.Add(name);
        result.Add(declaration);
    }

    foreach (var name in localSetups.Keys)
    {
        if (Regex.IsMatch(source, $@"\b{Regex.Escape(name)}\b"))
        {
            Add(name);
        }
    }

    return result;
}

void ReadDocumentedPackages(IReadOnlyList<string> lines)
{
    for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
    {
        if (!Regex.IsMatch(lines[lineIndex], "^\\s*```(?:bash|sh|shell|powershell|pwsh)\\s*$"))
        {
            continue;
        }

        for (lineIndex++; lineIndex < lines.Count && !Regex.IsMatch(lines[lineIndex], "^\\s*```\\s*$"); lineIndex++)
        {
            var match = Regex.Match(lines[lineIndex].Trim(), "^dotnet add package\\s+(TUnit(?:\\.[A-Za-z0-9_.-]+)?)\\b");
            if (match.Success)
            {
                documentedPackages.Add(match.Groups[1].Value);
            }
        }
    }
}

static (IReadOnlyList<string> Usings, string Body) ExtractUsings(string source)
{
    var usings = new List<string>();
    var body = new List<string>();
    foreach (var line in source.Split('\n'))
    {
        if (Regex.IsMatch(line, "^\\s*(?:global\\s+)?using\\s+(?!var\\b|\\()[^;]+;\\s*(?://.*)?$"))
        {
            usings.Add(line.Trim());
            body.Add(string.Empty);
        }
        else
        {
            body.Add(line);
        }
    }

    return (usings, string.Join('\n', body));
}

static SnippetMode Classify(string source, string path, int ordinal)
{
    var hasNamespaceLevelDeclaration = Regex.IsMatch(
        source,
        "(?m)^\\s*(?:file\\s+)?(?:public\\s+|internal\\s+|private\\s+|protected\\s+|static\\s+|sealed\\s+|abstract\\s+|partial\\s+|readonly\\s+|ref\\s+)*(?:class|record|struct|interface|enum|namespace)\\b");

    var candidates = hasNamespaceLevelDeclaration
        ? new[] { SnippetMode.Declaration, SnippetMode.Member, SnippetMode.Statements }
        : new[] { SnippetMode.Member, SnippetMode.Statements, SnippetMode.Declaration };

    foreach (var candidate in candidates)
    {
        if (candidate == SnippetMode.Declaration)
        {
            var root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)).GetCompilationUnitRoot();
            if (root.Members.Any(member => member is GlobalStatementSyntax))
            {
                continue;
            }
        }

        if (candidate == SnippetMode.Member && !Regex.IsMatch(
                Regex.Replace(source, "(?m)^\\s*//.*$", string.Empty).TrimStart(),
                "^(?:\\[|public\\s|internal\\s|private\\s|protected\\s|static\\s|sealed\\s|abstract\\s|partial\\s|readonly\\s|const\\s|event\\s)"))
        {
            continue;
        }

        var candidateSource = candidate switch
        {
            SnippetMode.Declaration => source,
            SnippetMode.Member => $"class DocumentationSnippet\n{{\n{source}\n}}",
            SnippetMode.Statements => $"class DocumentationSnippet\n{{\nasync Task CompileAsync()\n{{\n{source}\n}}\n}}",
            _ => throw new ArgumentOutOfRangeException()
        };

        if (!CSharpSyntaxTree.ParseText(candidateSource, new CSharpParseOptions(LanguageVersion.Preview))
            .GetDiagnostics()
            .Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return candidate;
        }
    }

    throw new InvalidOperationException($"C# fence {path}#{ordinal} is not syntactically valid in any supported context.");
}

static SnippetMode ClassifyWithUsageSplit(string source, string path, int ordinal, out string? splitBefore)
{
    var usageMarker = Regex.Match(
        source,
        "(?m)^//\\s*(?:Usage(?: in tests)?|Example usage|Using the assertion|In tests?):?\\s*$",
        RegexOptions.IgnoreCase);
    if (usageMarker.Success)
    {
        var declarationOrMemberSource = source[..usageMarker.Index].TrimEnd();
        splitBefore = usageMarker.Value;
        return Classify(declarationOrMemberSource, path, ordinal);
    }

    try
    {
        splitBefore = null;
        return Classify(source, path, ordinal);
    }
    catch (InvalidOperationException)
    {
        var root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)).GetCompilationUnitRoot();
        var firstGlobalStatement = root.Members.OfType<GlobalStatementSyntax>().FirstOrDefault();
        if (firstGlobalStatement is not null && root.Members.Any(member => member is not GlobalStatementSyntax))
        {
            var splitIndex = firstGlobalStatement.FullSpan.Start;
            var declarationSource = source[..splitIndex].TrimEnd();
            splitBefore = source[splitIndex..];
            return Classify(declarationSource, path, ordinal);
        }

        throw;
    }
}

static string GenerateSource(
    Snippet snippet,
    int index,
    ISet<string> emittedAssemblyAttributes,
    string locallyDeclaredSource)
{
    var generatedNamespace = $"TUnit.DocTests.Snippets.{snippet.SharedDocumentId ?? $"Snippet{index}"}";
    var wrapperName = $"DocumentationSnippet{index}";
    var wrapperAccessibility = Regex.IsMatch(snippet.Source, @"\[(?:Fact|Theory)\b") ? "public" : "internal";
    var builder = new StringBuilder()
        .AppendLine("// <auto-generated />")
        .AppendLine("#nullable enable");

    AppendFrameworkAliases(builder, snippet);

    if (snippet.Source.Contains("EditorBrowsable", StringComparison.Ordinal) &&
        !snippet.Usings.Any(usingDirective => usingDirective.Contains("System.ComponentModel", StringComparison.Ordinal)))
    {
        builder.AppendLine("using System.ComponentModel;");
    }

    foreach (var usingDirective in snippet.Usings)
    {
        builder.AppendLine(usingDirective);
    }

    var source = snippet.Source;
    if (snippet.Mode == SnippetMode.Declaration)
    {
        var assemblyAttribute = new Regex("(?m)^\\s*\\[assembly:\\s*[^\\r\\n]+\\]\\s*$");
        foreach (Match match in assemblyAttribute.Matches(source))
        {
            var attribute = QualifyLocallyDeclaredAttributeTypes(match.Value.Trim(), locallyDeclaredSource, generatedNamespace);
            if (emittedAssemblyAttributes.Add(attribute))
            {
                builder.AppendLine(attribute);
            }
        }
        source = assemblyAttribute.Replace(source, string.Empty);

        string? declarationTailSource = null;
        var statementLine = snippet.Line;
        if (snippet.SplitBefore is not null)
        {
            var splitIndex = source.IndexOf(snippet.SplitBefore, StringComparison.Ordinal);
            if (splitIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Declaration split marker '{snippet.SplitBefore}' was not found in {snippet.SourcePath}:{snippet.Line}.");
            }

            declarationTailSource = source[splitIndex..];
            statementLine += source[..splitIndex].Count(character => character == '\n');
            source = source[..splitIndex].TrimEnd();
        }

        var fileScopedNamespace = new Regex("(?m)^\\s*namespace\\s+([A-Za-z_][A-Za-z0-9_.]*)\\s*;");
        if (fileScopedNamespace.IsMatch(source))
        {
            source = fileScopedNamespace.Replace(source, $"namespace {generatedNamespace}.$1;", 1);
        }
        else
        {
            builder.AppendLine($"namespace {generatedNamespace};");
        }

        builder.AppendLine($"#line {snippet.Line} \"{snippet.SourcePath}\"")
            .AppendLine(source);
        if (declarationTailSource is not null)
        {
            var tailMode = Classify(declarationTailSource, snippet.SourcePath, index + 1);
            if (tailMode == SnippetMode.Declaration)
            {
                builder.AppendLine($"#line {statementLine} \"{snippet.SourcePath}\"")
                    .AppendLine(declarationTailSource);
            }
            else
            {
                builder.AppendLine($"{wrapperAccessibility} sealed class {wrapperName} : global::TUnit.DocTests.SnippetContext")
                    .AppendLine("{");
                if (tailMode == SnippetMode.Statements)
                {
                    builder.AppendLine("    public async Task CompileAsync()")
                        .AppendLine("    {");
                }
                builder.AppendLine($"#line {statementLine} \"{snippet.SourcePath}\"");
                var indentation = tailMode == SnippetMode.Statements ? "        " : "    ";
                foreach (var line in declarationTailSource.Split('\n'))
                {
                    builder.Append(indentation).AppendLine(line);
                }
                if (tailMode == SnippetMode.Statements)
                {
                    builder.AppendLine("    }");
                }
                builder.AppendLine("}");
            }
        }
        return builder.ToString();
    }

    if (snippet.Mode == SnippetMode.Statements && snippet.SplitBefore is not null)
    {
        throw new InvalidOperationException(
            $"Statement snippet {snippet.SourcePath}:{snippet.Line} cannot have a split marker.");
    }

    var memberSource = source;
    string? statementSource = null;
    var splitStatementLine = snippet.Line;
    if (snippet.SplitBefore is not null)
    {
        var splitIndex = source.IndexOf(snippet.SplitBefore, StringComparison.Ordinal);
        if (splitIndex < 0)
        {
            throw new InvalidOperationException(
                $"Member split marker '{snippet.SplitBefore}' was not found in {snippet.SourcePath}:{snippet.Line}.");
        }

        memberSource = source[..splitIndex].TrimEnd();
        statementSource = source[splitIndex..];
        splitStatementLine += source[..splitIndex].Count(character => character == '\n');
    }

    var containsExtensionMethod = Regex.IsMatch(memberSource, @"(?:\(|,)\s*this\s+[A-Za-z_]");
    if (snippet.Mode == SnippetMode.Statements && containsExtensionMethod)
    {
        throw new InvalidOperationException(
            $"Statement snippet {snippet.SourcePath}:{snippet.Line} cannot contain an extension method.");
    }

    builder.AppendLine($"namespace {generatedNamespace};");

    if (containsExtensionMethod)
    {
        builder.AppendLine($"internal static class DocumentationMembers{index}")
            .AppendLine("{")
            .AppendLine($"#line {snippet.Line} \"{snippet.SourcePath}\"");
        foreach (var line in memberSource.Split('\n'))
        {
            builder.Append("    ").AppendLine(line);
        }
        builder.AppendLine("}");

        if (statementSource is null)
        {
            return builder.ToString();
        }

        builder.AppendLine($"{wrapperAccessibility} sealed class {wrapperName} : global::TUnit.DocTests.SnippetContext")
            .AppendLine("{");
    }
    else
    {
        builder.AppendLine($"{wrapperAccessibility} sealed class {wrapperName} : global::TUnit.DocTests.SnippetContext")
            .AppendLine("{");
    }

    if (snippet.Mode == SnippetMode.Statements)
    {
        builder.AppendLine("    public async Task CompileAsync()")
            .AppendLine("    {");
        if (!string.IsNullOrWhiteSpace(snippet.Prelude))
        {
            builder.AppendLine("#line hidden");
            foreach (var line in snippet.Prelude.Split('\n'))
            {
                builder.Append("        ").AppendLine(line);
            }
        }
    }

    if (!containsExtensionMethod)
    {
        builder.AppendLine($"#line {snippet.Line} \"{snippet.SourcePath}\"");
        var indentation = snippet.Mode == SnippetMode.Statements ? "        " : "    ";
        foreach (var line in memberSource.Split('\n'))
        {
            builder.Append(indentation).AppendLine(line);
        }
    }

    if (snippet.Mode == SnippetMode.Statements)
    {
        builder.AppendLine("    }");
    }

    if (statementSource is not null)
    {
        builder.AppendLine("    public static async Task CompileAsync()")
            .AppendLine("    {")
            .AppendLine($"#line {splitStatementLine} \"{snippet.SourcePath}\"");
        foreach (var line in statementSource.Split('\n'))
        {
            builder.Append("        ").AppendLine(line);
        }
        builder.AppendLine("    }");
    }

    return builder.AppendLine("}").ToString();
}

static IReadOnlyList<(string Source, int Line)> SplitFrameworkSections(string source, int startLine)
{
    var markers = Regex.Matches(source, @"(?m)^//\s*(?:MSTest|NUnit|xUnit|TUnit)\s*$");
    if (markers.Count < 2)
    {
        return [(source, startLine)];
    }

    var sections = new List<(string Source, int Line)>();
    if (!string.IsNullOrWhiteSpace(source[..markers[0].Index]))
    {
        sections.Add((source[..markers[0].Index], startLine));
    }

    for (var index = 0; index < markers.Count; index++)
    {
        var marker = markers[index];
        var end = index + 1 < markers.Count ? markers[index + 1].Index : source.Length;
        var line = startLine + source[..marker.Index].Count(character => character == '\n');
        sections.Add((source[marker.Index..end].TrimEnd(), line));
    }

    return sections;
}

static Dictionary<string, string> GetLocalDeclarations(string source)
{
    var wrappedSource = $"class DocumentationSnippet {{ async Task CompileAsync() {{ {source} }} }}";
    var root = CSharpSyntaxTree.ParseText(wrappedSource, new CSharpParseOptions(LanguageVersion.Preview))
        .GetCompilationUnitRoot();
    var declarations = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var statement in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
    {
        foreach (var variable in statement.Declaration.Variables)
        {
            declarations[variable.Identifier.ValueText] = statement.ToFullString().Trim();
        }
    }

    return declarations;
}

static bool RequiresIsolatedCompilation(string source)
    => Regex.IsMatch(
        source,
        @"\[assembly:|\[(?:GenerateAssertion|AssertionFrom|AssertionExtension|ShouldName|GenerateMock)(?:Attribute)?(?:<|\(|\])|\.Mock\(\)|\bMock\.Of<");

static void AppendFrameworkAliases(StringBuilder builder, Snippet snippet)
{
    if (snippet.SourcePath.EndsWith("/migration/xunit.md", StringComparison.OrdinalIgnoreCase) &&
        Regex.IsMatch(snippet.Source, @"//\s*xUnit|\[(?:Fact|Theory|InlineData|MemberData|ClassData|Trait|Collection)\b|\bI(?:Class|Collection)Fixture<|\bIAsyncLifetime\b|\bITestOutputHelper\b"))
    {
        builder.AppendLine("using Assert = global::Xunit.Assert;")
            .AppendLine("using FactAttribute = global::Xunit.FactAttribute;")
            .AppendLine("using TheoryAttribute = global::Xunit.TheoryAttribute;")
            .AppendLine("using InlineDataAttribute = global::Xunit.InlineDataAttribute;")
            .AppendLine("using MemberDataAttribute = global::Xunit.MemberDataAttribute;")
            .AppendLine("using ClassDataAttribute = global::Xunit.ClassDataAttribute;")
            .AppendLine("using TraitAttribute = global::Xunit.TraitAttribute;")
            .AppendLine("using CollectionAttribute = global::Xunit.CollectionAttribute;")
            .AppendLine("using CollectionDefinitionAttribute = global::Xunit.CollectionDefinitionAttribute;")
            .AppendLine("using Xunit;");
    }

    if (snippet.SourcePath.EndsWith("/migration/nunit.md", StringComparison.OrdinalIgnoreCase) &&
        Regex.IsMatch(snippet.Source, @"//\s*NUnit|\[(?:TestFixture|TestCase|TestCaseSource|SetUp|TearDown|OneTimeSetUp|OneTimeTearDown|SetUpFixture|Values|Range)|NUnit\.Framework|\b(?:CollectionAssert|StringAssert|Is|Does|Has)\.|\bAssert\.(?:AreEqual|Greater|IsNull|IsTrue)|\bTestContext\.(?:CurrentContext|WriteLine|Out)"))
    {
        builder.AppendLine(snippet.Source.Contains("Assert.That", StringComparison.Ordinal)
                ? "using Assert = global::NUnit.Framework.Assert;"
                : "using Assert = global::NUnit.Framework.Legacy.ClassicAssert;")
            .AppendLine("using TestAttribute = global::NUnit.Framework.TestAttribute;")
            .AppendLine("using TestFixtureAttribute = global::NUnit.Framework.TestFixtureAttribute;")
            .AppendLine("using TestCaseAttribute = global::NUnit.Framework.TestCaseAttribute;")
            .AppendLine("using TestCaseSourceAttribute = global::NUnit.Framework.TestCaseSourceAttribute;")
            .AppendLine("using SetUpAttribute = global::NUnit.Framework.SetUpAttribute;")
            .AppendLine("using TearDownAttribute = global::NUnit.Framework.TearDownAttribute;")
            .AppendLine("using OneTimeSetUpAttribute = global::NUnit.Framework.OneTimeSetUpAttribute;")
            .AppendLine("using OneTimeTearDownAttribute = global::NUnit.Framework.OneTimeTearDownAttribute;")
            .AppendLine("using SetUpFixtureAttribute = global::NUnit.Framework.SetUpFixtureAttribute;")
            .AppendLine("using ValuesAttribute = global::NUnit.Framework.ValuesAttribute;")
            .AppendLine("using RangeAttribute = global::NUnit.Framework.RangeAttribute;")
            .AppendLine("using TestContext = global::NUnit.Framework.TestContext;")
            .AppendLine("using CollectionAssert = global::NUnit.Framework.Legacy.CollectionAssert;")
            .AppendLine("using StringAssert = global::NUnit.Framework.Legacy.StringAssert;")
            .AppendLine("using Is = global::NUnit.Framework.Is;")
            .AppendLine("using Does = global::NUnit.Framework.Does;")
            .AppendLine("using Has = global::NUnit.Framework.Has;");
    }

    if (snippet.SourcePath.EndsWith("/migration/mstest.md", StringComparison.OrdinalIgnoreCase) &&
        Regex.IsMatch(snippet.Source, @"//\s*MSTest|\[(?:TestClass|TestMethod|DataTestMethod|DataRow|DynamicData|TestInitialize|TestCleanup|ClassInitialize|ClassCleanup|AssemblyInitialize|AssemblyCleanup|ExpectedException|DeploymentItem|Owner|Priority|TestCategory|TestProperty)|Microsoft\.VisualStudio\.TestTools"))
    {
        builder.AppendLine("using Assert = global::Microsoft.VisualStudio.TestTools.UnitTesting.Assert;")
            .AppendLine("using TestContext = global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext;")
            .AppendLine("using CollectionAssert = global::Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert;")
            .AppendLine("using StringAssert = global::Microsoft.VisualStudio.TestTools.UnitTesting.StringAssert;")
            .AppendLine("using TestClassAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;")
            .AppendLine("using TestMethodAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;")
            .AppendLine("using DataTestMethodAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute;")
            .AppendLine("using DataRowAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.DataRowAttribute;")
            .AppendLine("using DynamicDataAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute;")
            .AppendLine("using DynamicDataSourceType = global::Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataSourceType;")
            .AppendLine("using TestInitializeAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute;")
            .AppendLine("using TestCleanupAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute;")
            .AppendLine("using ClassInitializeAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute;")
            .AppendLine("using ClassCleanupAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute;")
            .AppendLine("using AssemblyInitializeAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.AssemblyInitializeAttribute;")
            .AppendLine("using AssemblyCleanupAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.AssemblyCleanupAttribute;")
            .AppendLine("using DeploymentItemAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.DeploymentItemAttribute;")
            .AppendLine("using OwnerAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.OwnerAttribute;")
            .AppendLine("using PriorityAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.PriorityAttribute;")
            .AppendLine("using TestCategoryAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute;")
            .AppendLine("using TestPropertyAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute;");
        builder.AppendLine("using IgnoreAttribute = global::Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute;");
    }
}

static string QualifyLocallyDeclaredAttributeTypes(string attribute, string source, string generatedNamespace)
{
    var declaredTypes = Regex.Matches(
            source,
            "(?m)\\b(?:class|record|struct|interface|enum)\\s+([A-Za-z_][A-Za-z0-9_]*)")
        .Select(match => match.Groups[1].Value)
        .ToHashSet(StringComparer.Ordinal);

    foreach (var declaredType in declaredTypes)
    {
        var attributeNames = declaredType.EndsWith("Attribute", StringComparison.Ordinal)
            ? new[] { declaredType, declaredType[..^"Attribute".Length] }
            : new[] { declaredType };
        foreach (var attributeName in attributeNames)
        {
            attribute = Regex.Replace(
                attribute,
                $@"(\[assembly:\s*){Regex.Escape(attributeName)}(?=\s*(?:\(|\]))",
                $"$1{generatedNamespace}.{declaredType}");
        }

        attribute = Regex.Replace(
            attribute,
            $"(?<=<){Regex.Escape(declaredType)}(?=>)",
            $"{generatedNamespace}.{declaredType}");
        attribute = Regex.Replace(
            attribute,
            $"(?<=typeof\\(){Regex.Escape(declaredType)}(?=\\))",
            $"{generatedNamespace}.{declaredType}");
    }

    return attribute;
}

internal sealed record Snippet(
    string SourcePath,
    string? SharedDocumentId,
    int Line,
    IReadOnlyList<string> Usings,
    string Prelude,
    string Source,
    SnippetMode Mode,
    string? SplitBefore);

internal enum SnippetMode
{
    Declaration,
    Member,
    Statements
}
