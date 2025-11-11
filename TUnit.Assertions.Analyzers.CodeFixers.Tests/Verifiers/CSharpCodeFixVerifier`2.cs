using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using TUnit.Assertions;
using TUnit.Assertions.Analyzers.CodeFixers.Tests.Extensions;
using TUnit.Core;

namespace TUnit.Assertions.Analyzers.CodeFixers.Tests.Verifiers;

public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    private static ReferenceAssemblies GetReferenceAssemblies()
    {
#if NET472
        return ReferenceAssemblies.NetFramework.Net472.Default;
#elif NET8_0
        return ReferenceAssemblies.Net.Net80;
#elif NET9_0 || NET10_0_OR_GREATER
        return ReferenceAssemblies.Net.Net90;
#else
        return ReferenceAssemblies.Net.Net80; // Default fallback
#endif
    }
    /// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic"/>
    public static DiagnosticResult Diagnostic()
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic();

    /// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic"/>
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);

    /// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic"/>
    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor);

    public static async Task VerifyAnalyzerAsync(
        [StringSyntax("c#-test")] string source,
        params DiagnosticResult[] expected
    )
    {
        var referenceAssemblies = GetReferenceAssemblies();

        // Only add xunit package for XUnitAssertionAnalyzer
        if (typeof(TAnalyzer).Name == "XUnitAssertionAnalyzer")
        {
            referenceAssemblies = referenceAssemblies.AddPackages([new PackageIdentity("xunit.v3.assert", "3.2.0")]);
        }

        var test = new Test
        {
            TestCode = source.NormalizeLineEndings(),
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            ReferenceAssemblies = referenceAssemblies,
            TestState =
            {
                AdditionalReferences =
                {
                    typeof(TUnitAttribute).Assembly.Location,
                    typeof(Assert).Assembly.Location,
                },
            },
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, string)"/>
    public static async Task VerifyCodeFixAsync([StringSyntax("c#-test")] string source, [StringSyntax("c#-test")] string fixedSource)
        => await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

    /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)"/>
    public static async Task VerifyCodeFixAsync([StringSyntax("c#-test")] string source, DiagnosticResult expected, [StringSyntax("c#-test")] string fixedSource)
        => await VerifyCodeFixAsync(source, [expected], fixedSource);

    /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
    public static async Task VerifyCodeFixAsync(
        [StringSyntax("c#-test")] string source,
        IEnumerable<DiagnosticResult> expected,
        [StringSyntax("c#-test")] string fixedSource
    )
    {
        var referenceAssemblies = GetReferenceAssemblies();

        // Only add xunit package for XUnitAssertionAnalyzer
        if (typeof(TAnalyzer).Name == "XUnitAssertionAnalyzer")
        {
            referenceAssemblies = referenceAssemblies.AddPackages([new PackageIdentity("xunit.v3.assert", "3.2.0")]);
        }

        var test = new Test
        {
            TestCode = source.NormalizeLineEndings(),
            FixedCode = fixedSource.NormalizeLineEndings(),
            ReferenceAssemblies = referenceAssemblies,
            TestState =
            {
                AdditionalReferences =
                {
                    typeof(TUnitAttribute).Assembly.Location,
                    typeof(Assert).Assembly.Location,
                },
            },
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            CompilerDiagnostics = CompilerDiagnostics.None
        };

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync(CancellationToken.None);
    }
}
