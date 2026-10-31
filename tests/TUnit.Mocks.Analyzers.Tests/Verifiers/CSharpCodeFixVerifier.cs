using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace TUnit.Mocks.Analyzers.Tests.Verifiers;

public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, LineEndingNormalizingVerifier>.Diagnostic(descriptor);

    /// <summary>
    /// Verifies the diagnostics in <paramref name="source"/> and that applying every fix (one at a
    /// time and via Fix All) produces <paramref name="fixedSource"/>. <paramref name="stubsSource"/>
    /// is added unchanged to both states, so the fixed code must compile against it.
    /// </summary>
    public static async Task VerifyCodeFixAsync(
        [StringSyntax("c#")] string source,
        [StringSyntax("c#")] string fixedSource,
        [StringSyntax("c#")] string stubsSource,
        params DiagnosticResult[] expected)
    {
        var test = CreateTest(source, stubsSource, LanguageVersion.Preview);
        test.FixedCode = LineEndingNormalizingVerifier.NormalizeLineEndings(fixedSource);
        test.FixedState.Sources.Add(("Stubs.cs", LineEndingNormalizingVerifier.NormalizeLineEndings(stubsSource)));
        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync(CancellationToken.None);
    }

    /// <summary>
    /// Verifies that the analyzer reports nothing for <paramref name="source"/>.
    /// </summary>
    public static async Task VerifyNoDiagnosticAsync(
        [StringSyntax("c#")] string source,
        [StringSyntax("c#")] string stubsSource,
        LanguageVersion languageVersion = LanguageVersion.Preview,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        var test = CreateTest(source, stubsSource, languageVersion);
        test.TestState.OutputKind = outputKind;
        await test.RunAsync(CancellationToken.None);
    }

    private static CSharpCodeFixTest<TAnalyzer, TCodeFix, LineEndingNormalizingVerifier> CreateTest(
        string source,
        string stubsSource,
        LanguageVersion languageVersion)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, LineEndingNormalizingVerifier>
        {
            TestCode = LineEndingNormalizingVerifier.NormalizeLineEndings(source),
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("Stubs.cs", LineEndingNormalizingVerifier.NormalizeLineEndings(stubsSource)));

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

            return solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(languageVersion));
        });

        return test;
    }
}
