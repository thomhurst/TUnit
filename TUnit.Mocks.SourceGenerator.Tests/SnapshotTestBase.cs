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

    private static List<PortableExecutableReference> LoadReferences()
    {
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        // Add TUnit.Mocks.dll from the ref subfolder (netstandard2.0 build)
        var refDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ref");
        var mockDll = Path.Combine(refDir, "TUnit.Mocks.dll");
        if (File.Exists(mockDll))
        {
            refs.Add(MetadataReference.CreateFromFile(mockDll));
        }

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
            .OrderBy(t => t.FilePath, StringComparer.Ordinal)
            .Select(t => t.GetText().ToString())
            .ToArray();
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
                $"Update the .verified.txt file if this change is intentional.");
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
}
