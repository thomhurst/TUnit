using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace TUnit.Assertions.Analyzers.CodeFixers.Tests.Verifiers;

public static partial class CSharpCodeRefactoringVerifier<TCodeRefactoring>
    where TCodeRefactoring : CodeRefactoringProvider, new()
{
    public class Test : CSharpCodeRefactoringTest<TCodeRefactoring, DefaultVerifier>
    {
        public Test()
        {
            SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);

                if (project is null)
                {
                    return solution;
                }

                var compilationOptions = project.CompilationOptions;

                if (compilationOptions is null)
                {
                    return solution;
                }

                var parseOptions = project.ParseOptions as CSharpParseOptions;

                if (parseOptions is null)
                {
                    return solution;
                }

                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));

                solution = solution.WithProjectCompilationOptions(projectId, compilationOptions)
                    .WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion.Preview));

                return solution;
            });
        }
    }
}