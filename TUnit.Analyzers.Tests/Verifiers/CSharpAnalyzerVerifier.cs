using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace TUnit.Analyzers.Tests.Verifiers;

public static partial class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    {
        public Test()
        {
            ReferenceAssemblies.AddAssemblies(ReferenceAssemblies.Net.Net60.Assemblies);
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

                if (compilationOptions is CSharpCompilationOptions cSharpCompilationOptions)
                {
                    compilationOptions =
                        cSharpCompilationOptions.WithNullableContextOptions(NullableContextOptions.Enable);
                }

                if (project.ParseOptions is not CSharpParseOptions parseOptions)
                {
                    return solution;
                }

                compilationOptions = compilationOptions
                    .WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));

                solution = solution.WithProjectCompilationOptions(projectId, compilationOptions)
                    .WithProjectParseOptions(projectId, parseOptions
                        .WithLanguageVersion(LanguageVersion.Preview));

                return solution;
            });
        }
    }
}
