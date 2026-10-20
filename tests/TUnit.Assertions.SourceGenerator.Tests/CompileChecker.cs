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
#if NETFRAMEWORK
    // Generated code uses attributes that .NET Framework does not ship
    // (CallerArgumentExpressionAttribute, UnconditionalSuppressMessageAttribute). A real
    // .NET Framework consumer gets them from the Polyfill package, which declares them as
    // source in the consumer's own compilation (the TUnit template adds that package for
    // .NET Framework). The test's reference set cannot supply them: Polyfill embeds its
    // copies in the test assembly as internal types, so they are invisible to this
    // compilation. Declare them here the same way Polyfill does for a consumer.
    private const string NetFrameworkPolyfills =
        """
        namespace System.Runtime.CompilerServices
        {
            [System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
            internal sealed class CallerArgumentExpressionAttribute : System.Attribute
            {
                public CallerArgumentExpressionAttribute(string parameterName) => ParameterName = parameterName;
                public string ParameterName { get; }
            }
        }

        namespace System.Diagnostics.CodeAnalysis
        {
            [System.AttributeUsage(System.AttributeTargets.All, Inherited = false, AllowMultiple = true)]
            internal sealed class UnconditionalSuppressMessageAttribute : System.Attribute
            {
                public UnconditionalSuppressMessageAttribute(string category, string checkId)
                {
                    Category = category;
                    CheckId = checkId;
                }

                public string Category { get; }
                public string CheckId { get; }
                public string Scope { get; set; }
                public string Target { get; set; }
                public string MessageId { get; set; }
                public string Justification { get; set; }
            }
        }
        """;
#endif

    public static async Task AssertNoErrors(IEnumerable<string> generatedFiles)
    {
        var trees = generatedFiles
#if NETFRAMEWORK
            .Append(NetFrameworkPolyfills)
#endif
            .Select(source => CSharpSyntaxTree.ParseText(source))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "CompileCheck",
            trees,
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"{d.Id}: {d.GetMessage()}")
            .ToArray();

        await Assert.That(errors).IsEmpty();
    }
}
