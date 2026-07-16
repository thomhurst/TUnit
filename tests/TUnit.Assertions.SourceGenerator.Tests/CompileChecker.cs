using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Assertions.SourceGenerator.Tests;

/// <summary>
/// Parses generator output as C# and asserts there are no error-severity diagnostics.
/// Pins the entire class of source-generator emit bugs (mis-paired brackets, wrong
/// default-value rendering, invalid generic parameter lists) regardless of the specific
/// diagnostic id. Use alongside content-shape assertions when the test wants both a
/// targeted regression check and a structural compile-clean gate.
/// </summary>
internal static class CompileChecker
{
    public static async Task AssertNoErrors(IEnumerable<string> generatedFiles)
    {
        var trees = generatedFiles
            .Select(source => CSharpSyntaxTree.ParseText(source))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "CompileCheck",
            trees,
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // On the net472 leg of CI, the Polyfill assembly's CallerArgumentExpressionAttribute
        // is declared internal, which produces a CS0122 ('inaccessible due to its protection
        // level') false positive when Roslyn compiles the generator's output through the test
        // project's reference set. Filter CS0122 only on net472; the emit shape is still
        // pinned by the `.Net4_7` snapshot files. On modern TFMs the BCL attribute is public,
        // so CS0122 (if it ever appears) would be a genuine signal and is not filtered.
        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
#if NETFRAMEWORK
            .Where(d => !string.Equals(d.Id, "CS0122", System.StringComparison.Ordinal))
#endif
            .Select(d => $"{d.Id}: {d.GetMessage()}")
            .ToArray();

        await Assert.That(errors).IsEmpty();
    }
}
