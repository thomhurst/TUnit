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
var contextualSnippets = 0;
var contextualFiles = new HashSet<string>(StringComparer.Ordinal);
var excludedSnippets = 0;
var excludedFiles = 0;

foreach (var document in documents)
{
    ReadDocument(document);
}

Directory.CreateDirectory(outputDirectory);
foreach (var oldFile in Directory.EnumerateFiles(outputDirectory, "Snippet*.g.cs"))
{
    File.Delete(oldFile);
}

var emittedAssemblyAttributes = new HashSet<string>(StringComparer.Ordinal);
for (var index = 0; index < snippets.Count; index++)
{
    File.WriteAllText(
        Path.Combine(outputDirectory, $"Snippet{index}.g.cs"),
        GenerateSource(snippets[index], index, emittedAssemblyAttributes),
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

Console.WriteLine($"Generated {snippets.Count} C# documentation snippets.");
Console.WriteLine($"Skipped {contextualSnippets} snippets declared contextual by {contextualFiles.Count} files, {excludedSnippets} explicit snippets, and {excludedFiles} files.");
Console.WriteLine($"Documented TUnit packages: {string.Join(", ", documentedPackages.Order())}");
return 0;

void ReadDocument(string documentPath)
{
    var relativePath = Path.GetRelativePath(repositoryRoot, documentPath).Replace('\\', '/');
    var lines = File.ReadAllLines(documentPath);
    ReadDocumentedPackages(lines);
    var fileIgnoreDirective = lines
        .Select(line => Regex.Match(line.Trim(), "^<!--\\s*doc-test-ignore-file:\\s*(.+?)\\s*-->$"))
        .FirstOrDefault(match => match.Success);
    if (fileIgnoreDirective?.Success == true)
    {
        excludedFiles++;
        Console.WriteLine($"EXCLUDED {relativePath} ({fileIgnoreDirective.Groups[1].Value})");
        return;
    }

    var contextualFileDirective = lines
        .Select(line => Regex.Match(line.Trim(), "^<!--\\s*doc-test-contextual-file:\\s*(.+?)\\s*-->$"))
        .FirstOrDefault(match => match.Success);
    if (lines.Any(line => line.Trim().StartsWith("<!-- doc-test-contextual-file", StringComparison.Ordinal)) &&
        contextualFileDirective?.Success != true)
    {
        throw new InvalidOperationException(
            $"Malformed doc-test-contextual-file directive in {relativePath}. Include a non-empty reason.");
    }

    var csharpOrdinal = 0;

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

            var directive = startLine >= 3 ? lines[startLine - 3].Trim() : string.Empty;
            var ignoreMatch = Regex.Match(directive, "^<!--\\s*doc-test-ignore:\\s*(.+?)\\s*-->$");
            if (directive.StartsWith("<!-- doc-test-ignore", StringComparison.Ordinal) && !ignoreMatch.Success)
            {
                throw new InvalidOperationException(
                    $"Malformed doc-test-ignore directive before {relativePath}:{startLine}. Include a non-empty reason.");
            }

            if (ignoreMatch.Success)
            {
                excludedSnippets++;
                Console.WriteLine($"EXCLUDED {relativePath}#{csharpOrdinal} ({ignoreMatch.Groups[1].Value})");
                continue;
            }

            var source = string.Join('\n', body);
            source = Regex.Replace(source, "(?m)^\\s*#:[^\\r\\n]*$", string.Empty);
            source = Regex.Replace(source, "(?m)^([ \\t]*)(?:\\.\\.\\.|…)\\s*;?\\s*$", string.Empty);
            source = Regex.Replace(source, "\\{\\s*(?:\\.\\.\\.|…)\\s*\\}", "{ }");
            var sourceWithoutComments = Regex.Replace(source, "(?s)/\\*.*?\\*/", string.Empty);
            sourceWithoutComments = Regex.Replace(sourceWithoutComments, "(?m)//.*$", string.Empty);
            if (Regex.IsMatch(sourceWithoutComments, "(?:=>|=)\\s*(?:\\.\\.\\.|…)\\s*;"))
            {
                throw new InvalidOperationException(
                    $"Incomplete C# at {relativePath}#{csharpOrdinal} must have a doc-test-ignore directive with a reason.");
            }

            var explicitMode = Regex.Match(directive, "^<!--\\s*doc-test-(declaration|member|statements)\\s*-->$");
            var splitMode = Regex.Match(directive, "^<!--\\s*doc-test-(declaration|member):\\s*split-before=(.+?)\\s*-->$");
            if (directive.StartsWith("<!-- doc-test-", StringComparison.Ordinal) &&
                !explicitMode.Success &&
                !splitMode.Success)
            {
                throw new InvalidOperationException(
                    $"Unknown or malformed doc-test directive before {relativePath}:{startLine}.");
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

            if (mode != SnippetMode.Declaration &&
                !explicitMode.Success &&
                !splitMode.Success &&
                contextualFileDirective?.Success == true)
            {
                contextualSnippets++;
                contextualFiles.Add(relativePath);
                continue;
            }

            snippets.Add(new Snippet(
                documentPath.Replace('\\', '/'),
                startLine,
                usings,
                sourceWithoutUsings,
                mode,
                splitBefore));
            continue;
        }

    }
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

        var usageMarker = Regex.Match(
            source,
            "(?m)^//\\s*(?:Usage(?: in tests)?|Example usage|Using the assertion|In tests?):?\\s*$",
            RegexOptions.IgnoreCase);
        if (!usageMarker.Success)
        {
            throw;
        }

        var declarationOrMemberSource = source[..usageMarker.Index].TrimEnd();
        splitBefore = usageMarker.Value;
        return Classify(declarationOrMemberSource, path, ordinal);
    }
}

static string GenerateSource(Snippet snippet, int index, ISet<string> emittedAssemblyAttributes)
{
    var generatedNamespace = $"TUnit.DocTests.Snippets.Snippet{index}";
    var builder = new StringBuilder()
        .AppendLine("// <auto-generated />")
        .AppendLine("#pragma warning disable");

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
            var attribute = QualifyLocallyDeclaredAttributeTypes(match.Value.Trim(), source, generatedNamespace);
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
            builder.AppendLine("internal sealed class DocumentationSnippet : global::TUnit.DocTests.SnippetContext")
                .AppendLine("{")
                .AppendLine("    public static async Task CompileAsync()")
                .AppendLine("    {")
                .AppendLine($"#line {statementLine} \"{snippet.SourcePath}\"");
            foreach (var line in declarationTailSource.Split('\n'))
            {
                builder.Append("        ").AppendLine(line);
            }
            builder.AppendLine("    }")
                .AppendLine("}");
        }
        return builder.ToString();
    }

    var memberSource = source;
    string? statementSource = null;
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
    }

    var containsExtensionMethod = Regex.IsMatch(memberSource, @"(?:\(|,)\s*this\s+[A-Za-z_]");
    builder.AppendLine($"namespace {generatedNamespace};");

    if (containsExtensionMethod)
    {
        builder.AppendLine("internal static class DocumentationMembers")
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

        builder.AppendLine("internal sealed class DocumentationSnippet : global::TUnit.DocTests.SnippetContext")
            .AppendLine("{");
    }
    else
    {
        builder.AppendLine("internal sealed class DocumentationSnippet : global::TUnit.DocTests.SnippetContext")
            .AppendLine("{");
    }

    if (snippet.Mode == SnippetMode.Statements)
    {
        builder.AppendLine("    public static async Task CompileAsync()")
            .AppendLine("    {");
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
        var statementLine = snippet.Line + source[..source.IndexOf(snippet.SplitBefore!, StringComparison.Ordinal)].Count(character => character == '\n');
        builder.AppendLine("    public static async Task CompileAsync()")
            .AppendLine("    {")
            .AppendLine($"#line {statementLine} \"{snippet.SourcePath}\"");
        foreach (var line in statementSource.Split('\n'))
        {
            builder.Append("        ").AppendLine(line);
        }
        builder.AppendLine("    }");
    }

    return builder.AppendLine("}").ToString();
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
    int Line,
    IReadOnlyList<string> Usings,
    string Source,
    SnippetMode Mode,
    string? SplitBefore);

internal enum SnippetMode
{
    Declaration,
    Member,
    Statements
}
