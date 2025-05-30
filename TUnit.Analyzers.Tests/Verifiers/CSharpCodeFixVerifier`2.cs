using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Diagnostics.CodeAnalysis;
using TUnit.Analyzers.Tests.Extensions;
using TUnit.Core;

namespace TUnit.Analyzers.Tests.Verifiers;

public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    /// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic"/>
    public static DiagnosticResult Diagnostic()
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic();

    /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(string)"/>
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);

    /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor);

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
    public static Task VerifyAnalyzerAsync([StringSyntax("c#")] string source, params DiagnosticResult[] expected)
    {
        return VerifyAnalyzerAsync(source, _ => { }, expected);
    }
    
    /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
    public static async Task VerifyAnalyzerAsync(
        [StringSyntax("c#")] string source,
        Action<Test> configureTest,
        params DiagnosticResult[] expected
    )
    {
        var test = new Test
        {
            TestCode = source.NormalizeLineEndings(),
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
                .AddPackages([new PackageIdentity("xunit.v3.extensibility.core", "2.0.0")]),
            TestState =
            {
                AdditionalReferences =
                {
                    typeof(TUnitAttribute).Assembly.Location,
                },
            }
        };

        test.ExpectedDiagnostics.AddRange(expected);
        
        configureTest(test);
        
        await test.RunAsync(CancellationToken.None);
    }

    /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, string)"/>
    public static async Task VerifyCodeFixAsync([StringSyntax("c#")] string source, [StringSyntax("c#")] string fixedSource)
        => await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

    /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)"/>
    public static async Task VerifyCodeFixAsync([StringSyntax("c#")] string source, DiagnosticResult expected, [StringSyntax("c#")] string fixedSource)
        => await VerifyCodeFixAsync(source, [expected], fixedSource);

    public static async Task VerifyCodeFixAsync([StringSyntax("c#")] string source, DiagnosticResult expected, [StringSyntax("c#")] string fixedSource, Action<Test> configureTest)
        => await VerifyCodeFixAsync(source, [expected], fixedSource, configureTest);

    /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
    public static Task VerifyCodeFixAsync(
        [StringSyntax("c#")] string source,
        IEnumerable<DiagnosticResult> expected,
        [StringSyntax("c#")] string fixedSource
    )
    {
        return VerifyCodeFixAsync(source, expected, fixedSource, _ => { });
    }

    /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
    public static async Task VerifyCodeFixAsync(
        [StringSyntax("c#")] string source,
        IEnumerable<DiagnosticResult> expected,
        [StringSyntax("c#")] string fixedSource,
        Action<Test> configureTest
    )
    {
        var test = new Test
        {
            TestCode = source.NormalizeLineEndings(),
            FixedCode = fixedSource.NormalizeLineEndings(),
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            TestState =
            {
                AdditionalReferences =
                {
                    typeof(TUnitAttribute).Assembly.Location,
                },
            },
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            CompilerDiagnostics = CompilerDiagnostics.None
        };

        test.ExpectedDiagnostics.AddRange(expected);
        
        configureTest(test);
        
        await test.RunAsync(CancellationToken.None);
    }
}