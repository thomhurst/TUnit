using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Diagnostics.CodeAnalysis;
using Polly.CircuitBreaker;
using TUnit.Core;
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
    public static async Task VerifyAnalyzerAsync([StringSyntax("c#")] string source, params DiagnosticResult[] expected)
    {
        var test = new Test
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
                .AddPackages([new PackageIdentity("xunit.v3.extensibility.core", "2.0.0")]),
            TestState =
            {
                AdditionalReferences =
                {
                    typeof(TUnitAttribute).Assembly.Location,
                    typeof(CircuitState).Assembly.Location,
                    typeof(ProjectReferenceEnum).Assembly.Location,
                },
            },
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }
}