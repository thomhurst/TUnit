using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Analyzers.Tests.Verifiers;

public static partial class CSharpCodeRefactoringVerifier<TCodeRefactoring>
    where TCodeRefactoring : CodeRefactoringProvider, new()
{
    /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, string)"/>
    public static async Task VerifyRefactoringAsync([StringSyntax("c#-test")] string source, [StringSyntax("c#-test")] string fixedSource)
    {
        await VerifyRefactoringAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);
    }

    /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, DiagnosticResult, string)"/>
    public static async Task VerifyRefactoringAsync([StringSyntax("c#-test")] string source, DiagnosticResult expected, [StringSyntax("c#-test")] string fixedSource)
    {
        await VerifyRefactoringAsync(source, [expected], fixedSource);
    }

    /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, DiagnosticResult[], string)"/>
    public static async Task VerifyRefactoringAsync([StringSyntax("c#-test")] string source, DiagnosticResult[] expected, [StringSyntax("c#-test")] string fixedSource)
    {
        var test = new Test
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }
}