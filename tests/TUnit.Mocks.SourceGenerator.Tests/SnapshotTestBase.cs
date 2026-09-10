using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Base class for source generator snapshot tests.
/// Provides helpers to compile source, run the MockGenerator, and verify output.
/// </summary>
public abstract class SnapshotTestBase
{
    private static readonly Lazy<List<PortableExecutableReference>> _references = new(LoadReferences);
    private static readonly UTF8Encoding SnapshotEncoding = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Returns the shared, lazily-loaded set of metadata references used by the test compilations
    /// (current AppDomain assemblies, with exactly one TUnit.Mocks reference). Derived test classes
    /// should reuse this instead of re-discovering references per test invocation.
    /// </summary>
    protected static IEnumerable<MetadataReference> GetCachedReferences() => _references.Value;

    private static List<PortableExecutableReference> LoadReferences()
    {
        // TUnit.Mocks must appear exactly once. Two references to it — the loaded assembly plus a
        // second copy on disk — leave `Mock`/`GenerateMockAttribute` bound to an error type unless
        // the two are byte-identical, and neither Roslyn nor the generator reports that: the
        // generator simply finds no mock targets and emits only its post-init namespace stub, so
        // every affected test fails as a bare snapshot mismatch. That is what a `ref/` copy of the
        // netstandard2.0 build used to risk, and it broke the [assembly: GenerateMock] tests on
        // the windows and macos CI jobs while ubuntu stayed green.
        var mocksAssembly = typeof(global::TUnit.Mocks.Mock).Assembly;

        if (string.IsNullOrWhiteSpace(mocksAssembly.Location))
        {
            throw new InvalidOperationException(
                "No TUnit.Mocks reference available to the test compilations — the generator would silently produce nothing.");
        }

        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Where(a => a != mocksAssembly)
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        refs.Add(MetadataReference.CreateFromFile(mocksAssembly.Location));

        return refs;
    }

    /// <summary>
    /// Runs the MockGenerator against the given source and returns the generated files
    /// as an array of strings, ordered by hint name for stable snapshot comparison.
    /// </summary>
    protected static string[] RunGenerator(
        string source,
        IEnumerable<MetadataReference>? additionalReferences = null,
        CSharpParseOptions? parseOptions = null)
    {
        parseOptions ??= CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        IEnumerable<MetadataReference> refs = additionalReferences is null
            ? _references.Value
            : _references.Value.Concat(additionalReferences);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        ).WithReferences(refs);

        var generator = new MockGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // Check for generator errors
        var errors = runResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            var errorMessages = string.Join(Environment.NewLine, errors.Select(e => e.ToString()));
            throw new InvalidOperationException($"Generator produced errors:{Environment.NewLine}{errorMessages}");
        }

        return runResult.GeneratedTrees
            .OrderBy(t => GetGeneratedTreeSortKey(t.FilePath), StringComparer.Ordinal)
            .Select(t => t.GetText().ToString())
            .ToArray();
    }

    /// <summary>
    /// Runs the MockGenerator and returns its diagnostics alongside the generated files. Unlike
    /// <see cref="RunGenerator"/> this does not throw on generator errors — use it when the
    /// diagnostic is what the test is about.
    /// </summary>
    protected static (string[] Sources, IReadOnlyList<Diagnostic> Diagnostics) RunGeneratorForDiagnostics(
        string source,
        IEnumerable<MetadataReference>? additionalReferences = null,
        IIncrementalGenerator? generator = null)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        IEnumerable<MetadataReference> refs = additionalReferences is null
            ? _references.Value
            : _references.Value.Concat(additionalReferences);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        ).WithReferences(refs);

        generator ??= new MockGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var sources = runResult.GeneratedTrees
            .OrderBy(t => GetGeneratedTreeSortKey(t.FilePath), StringComparer.Ordinal)
            .Select(t => t.GetText().ToString())
            .ToArray();

        return (sources, runResult.Diagnostics);
    }

    private static string GetGeneratedTreeSortKey(string filePath)
    {
        var normalizedPath = filePath.Replace('\\', '/');
        var fileNameStart = normalizedPath.LastIndexOf('/');

        return fileNameStart >= 0
            ? normalizedPath[(fileNameStart + 1)..]
            : normalizedPath;
    }

    /// <summary>
    /// Compiles the given source into an in-memory assembly and returns it as a MetadataReference.
    /// Useful for simulating external assemblies in tests.
    /// </summary>
    protected static MetadataReference CreateExternalAssemblyReference(string source, string assemblyName = "ExternalLib")
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        ).WithReferences(_references.Value);

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);
        if (!emitResult.Success)
        {
            var errors = string.Join(Environment.NewLine, emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException($"Failed to compile external assembly '{assemblyName}':{Environment.NewLine}{errors}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        // CreateFromStream copies bytes immediately; ms is safely disposed after this call
        return MetadataReference.CreateFromStream(ms);
    }

    private static string RunGeneratorAndFormat(string source, IEnumerable<MetadataReference>? additionalReferences = null)
    {
        var files = RunGenerator(source, additionalReferences);
        var combined = string.Join("\n\n// ===== FILE SEPARATOR =====\n\n",
            files.Select(NormalizeNewlines));
        return combined;
    }

    protected static Task VerifyGeneratorOutput(
        string source,
        IEnumerable<MetadataReference> additionalReferences,
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        return VerifySnapshot(RunGeneratorAndFormat(source, additionalReferences), testName, filePath);
    }

    protected static Task VerifyGeneratorOutput(
        string source,
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        return VerifySnapshot(RunGeneratorAndFormat(source), testName, filePath);
    }

    protected static void AssertGeneratedCodeCompiles(
        string source,
        IEnumerable<MetadataReference>? additionalReferences = null,
        CSharpParseOptions? parseOptions = null)
    {
        var compileErrors = GetGeneratedCompilationErrors(source, additionalReferences, parseOptions);

        if (compileErrors.Count > 0)
        {
            var errorMessages = string.Join(Environment.NewLine, compileErrors.Select(e => e.ToString()));
            throw new InvalidOperationException($"Generated compilation failed:{Environment.NewLine}{errorMessages}");
        }
    }

    protected static IReadOnlyList<Diagnostic> GetGeneratedCompilationErrors(
        string source,
        IEnumerable<MetadataReference>? additionalReferences = null,
        CSharpParseOptions? parseOptions = null)
    {
        var (_, diagnostics) = RunGeneratorWithCompilationDiagnostics(source, additionalReferences, parseOptions);
        return diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
    }

    /// <summary>
    /// Compiles the generator output under nullable-enabled context and asserts that no
    /// CS86xx-family nullable warnings (CS8600, CS8602, CS8603, CS8604, CS8618, CS8625)
    /// are emitted. Used as a regression guard for #5626 and #5424/#5425/#5251.
    /// Other compiler diagnostics (e.g. CS1520 from C# 14 extension() blocks not parseable
    /// by the test-pinned Roslyn) are intentionally not asserted because they're
    /// limitations of the test infrastructure, not the generator output.
    /// </summary>
    protected static void AssertGeneratedCodeHasNoNullableWarnings(
        string source,
        IEnumerable<MetadataReference>? additionalReferences = null,
        CSharpParseOptions? parseOptions = null)
    {
        var (_, diagnostics) = RunGeneratorWithCompilationDiagnostics(source, additionalReferences, parseOptions);
        var nullableWarnings = diagnostics
            .Where(d => d.Severity >= DiagnosticSeverity.Warning && d.Id.StartsWith("CS86", StringComparison.Ordinal))
            .ToList();
        if (nullableWarnings.Count > 0)
        {
            var messages = string.Join(Environment.NewLine, nullableWarnings.Select(d => d.ToString()));
            throw new InvalidOperationException(
                $"Generated code emits {nullableWarnings.Count} CS86xx nullable warning(s):{Environment.NewLine}{messages}");
        }
    }

    /// <summary>
    /// Compiles the generator output and asserts no CS0612 (obsolete, no message), CS0618
    /// (obsolete with message), or CS0672 ('override missing Obsolete') warnings are emitted.
    /// Direct compile-time guard for the [Obsolete]-propagation portion of #5626. Proves
    /// the snapshot's [Obsolete] attribute placement actually suppresses the warnings, not
    /// just appears in the generated text.
    /// </summary>
    protected static void AssertGeneratedCodeHasNoObsoleteWarnings(
        string source,
        IEnumerable<MetadataReference>? additionalReferences = null,
        CSharpParseOptions? parseOptions = null)
    {
        var (_, diagnostics) = RunGeneratorWithCompilationDiagnostics(source, additionalReferences, parseOptions);
        var obsoleteWarnings = diagnostics
            .Where(d => d.Severity >= DiagnosticSeverity.Warning
                     && (string.Equals(d.Id, "CS0612", StringComparison.Ordinal)
                      || string.Equals(d.Id, "CS0618", StringComparison.Ordinal)
                      || string.Equals(d.Id, "CS0672", StringComparison.Ordinal)))
            .ToList();
        if (obsoleteWarnings.Count > 0)
        {
            var messages = string.Join(Environment.NewLine, obsoleteWarnings.Select(d => d.ToString()));
            throw new InvalidOperationException(
                $"Generated code emits {obsoleteWarnings.Count} obsolete warning(s):{Environment.NewLine}{messages}");
        }
    }

    private static (Compilation Compilation, IReadOnlyList<Diagnostic> Diagnostics) RunGeneratorWithCompilationDiagnostics(
        string source,
        IEnumerable<MetadataReference>? additionalReferences,
        CSharpParseOptions? parseOptions)
    {
        parseOptions ??= CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        IEnumerable<MetadataReference> refs = additionalReferences is null
            ? _references.Value
            : _references.Value.Concat(additionalReferences);

        var inputCompilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var generator = new MockGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var generatorDiagnostics);

        var generatorErrors = generatorDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (generatorErrors.Count > 0)
        {
            var errorMessages = string.Join(Environment.NewLine, generatorErrors.Select(e => e.ToString()));
            throw new InvalidOperationException($"Generator produced errors:{Environment.NewLine}{errorMessages}");
        }

        return (outputCompilation, outputCompilation.GetDiagnostics());
    }

    private static async Task VerifySnapshot(
        string generatedOutput,
        string testName,
        string filePath)
    {
        generatedOutput = NormalizeNewlines(generatedOutput).TrimStart('\uFEFF');

        var testDir = Path.GetDirectoryName(filePath)!;
        var receivedPath = Path.Combine(testDir, "Snapshots", $"{testName}.received.txt");
        var verifiedPath = Path.Combine(testDir, "Snapshots", $"{testName}.verified.txt");

        // Ensure Snapshots directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(receivedPath)!);

        if (!File.Exists(verifiedPath))
        {
            // Write .received.txt for review and fail — never auto-accept
            await File.WriteAllTextAsync(receivedPath, generatedOutput, SnapshotEncoding);
            throw new InvalidOperationException(
                $"No verified snapshot found for '{testName}'.\n" +
                $"Review: {receivedPath}\n" +
                $"Accept by renaming to '.verified.txt'.");
        }

        var verified = NormalizeNewlines(await File.ReadAllTextAsync(verifiedPath)).TrimStart('\uFEFF');

        if (!string.Equals(generatedOutput, verified, StringComparison.Ordinal))
        {
            await File.WriteAllTextAsync(receivedPath, generatedOutput, SnapshotEncoding);
            throw new InvalidOperationException(
                $"Snapshot mismatch for '{testName}'.\n" +
                $"Received: {receivedPath}\n" +
                $"Verified: {verifiedPath}\n" +
                $"Update the .verified.txt file if this change is intentional.\n" +
                $"{DescribeDifference(verified, generatedOutput)}");
        }

        // Clean up any leftover .received.txt on success
        if (File.Exists(receivedPath))
        {
            File.Delete(receivedPath);
        }
    }

    private static string NormalizeNewlines(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>
    /// Renders the first divergence between the two snapshots inline in the failure message.
    /// The <c>.received.txt</c> file is only useful to someone with the working tree in front of
    /// them; a mismatch that reproduces on CI (or on one OS only) is otherwise undiagnosable from
    /// the log alone.
    /// </summary>
    private static string DescribeDifference(string verified, string received)
    {
        const int ContextLines = 3;
        const int MaxReportedLines = 40;

        var verifiedLines = verified.Split('\n');
        var receivedLines = received.Split('\n');

        var firstDifference = 0;
        while (firstDifference < verifiedLines.Length
               && firstDifference < receivedLines.Length
               && string.Equals(verifiedLines[firstDifference], receivedLines[firstDifference], StringComparison.Ordinal))
        {
            firstDifference++;
        }

        var report = new StringBuilder();
        report.Append("Verified has ").Append(verifiedLines.Length)
            .Append(" line(s), received has ").Append(receivedLines.Length)
            .Append(" line(s); first difference at line ").Append(firstDifference + 1).Append('.')
            .Append('\n');

        var contextStart = Math.Max(0, firstDifference - ContextLines);
        for (var i = contextStart; i < firstDifference; i++)
        {
            report.Append("  ").Append(verifiedLines[i]).Append('\n');
        }

        var reported = 0;
        for (var i = firstDifference; i < Math.Max(verifiedLines.Length, receivedLines.Length) && reported < MaxReportedLines; i++)
        {
            var verifiedLine = i < verifiedLines.Length ? verifiedLines[i] : null;
            var receivedLine = i < receivedLines.Length ? receivedLines[i] : null;

            if (string.Equals(verifiedLine, receivedLine, StringComparison.Ordinal))
            {
                report.Append("  ").Append(verifiedLine).Append('\n');
                continue;
            }

            if (verifiedLine is not null)
            {
                report.Append("- ").Append(verifiedLine).Append('\n');
            }

            if (receivedLine is not null)
            {
                report.Append("+ ").Append(receivedLine).Append('\n');
            }

            reported++;
        }

        if (reported == MaxReportedLines)
        {
            report.Append("  ... (truncated)\n");
        }

        return report.ToString();
    }
}
