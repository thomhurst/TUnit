using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Polly.CircuitBreaker;
using TUnit.TestProject.Library;

namespace TUnit.Analyzers.Tests.Verifiers;

public static partial class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic"/>
    public static DiagnosticResult Diagnostic()
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic();

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic(string)"/>
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor);

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
    public static Task VerifyAnalyzerAsync([StringSyntax("c#")] string source, params DiagnosticResult[] expected)
    {
        return VerifyAnalyzerAsync(source, _ => { }, expected);
    }

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
    public static async Task VerifyAnalyzerAsync([StringSyntax("c#")] string source, Action<Test> configureTest, params DiagnosticResult[] expected)
    {
        // Microsoft.Bcl.AsyncInterfaces is required by TUnit.Core.netstandard2.0.dll's typeforward
        // of IAsyncEnumerable; without it, tests that use IAsyncEnumerable in their source see
        // CS0012/CS0508 alongside the expected analyzer diagnostic.
        var test = new Test
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
                .AddPackages([new PackageIdentity("Microsoft.Bcl.AsyncInterfaces", "9.0.0")]),
            TestState =
            {
                AdditionalReferences =
                {
                    AnalyzerTestHelpers.GetCompatibleDllPath("TUnit.Core", typeof(TUnitAttribute).Assembly),
                    typeof(CircuitState).Assembly.Location,
                    AnalyzerTestHelpers.GetCompatibleDllPath("TUnit.TestProject.Library", typeof(ProjectReferenceEnum).Assembly),
                },
            },
        };

        test.ExpectedDiagnostics.AddRange(expected);

        configureTest(test);

        await test.RunAsync(CancellationToken.None);
    }
}
