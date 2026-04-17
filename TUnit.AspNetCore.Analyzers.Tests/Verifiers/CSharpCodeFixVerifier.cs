using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace TUnit.AspNetCore.Analyzers.Tests.Verifiers;

public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, LineEndingNormalizingVerifier>.Diagnostic(descriptor);

    public static Task VerifyCodeFixAsync(
        [StringSyntax("c#")] string source,
        [StringSyntax("c#")] string fixedSource,
        params DiagnosticResult[] expected)
        => VerifyCodeFixAsync(source, fixedSource, stubsSource: null, expected);

    public static async Task VerifyCodeFixAsync(
        [StringSyntax("c#")] string source,
        [StringSyntax("c#")] string fixedSource,
        [StringSyntax("c#")] string? stubsSource,
        params DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, LineEndingNormalizingVerifier>
        {
            TestCode = LineEndingNormalizingVerifier.NormalizeLineEndings(source),
            FixedCode = LineEndingNormalizingVerifier.NormalizeLineEndings(fixedSource),
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        if (stubsSource is not null)
        {
            var normalizedStubs = LineEndingNormalizingVerifier.NormalizeLineEndings(stubsSource);
            test.TestState.Sources.Add(normalizedStubs);
            test.FixedState.Sources.Add(normalizedStubs);
        }

        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", SourceText.From("""
            is_global = true
            end_of_line = lf
            """)));

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);
            if (project?.ParseOptions is not CSharpParseOptions parseOptions)
            {
                return solution;
            }

            return solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion.Preview));
        });

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync(CancellationToken.None);
    }
}
