using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using TUnit.Assertions;
using TUnit.Core;

namespace TUnit.Assertions.Analyzers.Tests.Verifiers;

public static partial class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
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
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic();

    /// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic"/>
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

    /// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic"/>
    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor);

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
    public static async Task VerifyAnalyzerAsync([StringSyntax("c#-test")] string source, params DiagnosticResult[] expected)
    {
        var test = CreateTest(source);

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    /// <summary>
    /// Like <see cref="VerifyAnalyzerAsync(string, DiagnosticResult[])"/>, but the references whose file name
    /// is in <paramref name="aliasedReferenceFileNames"/> are only reachable through <c>extern alias</c>
    /// <paramref name="alias"/> (not merged into the global namespace).
    /// </summary>
    public static async Task VerifyAnalyzerWithAliasedReferencesAsync(
        [StringSyntax("c#-test")] string source,
        string alias,
        string[] aliasedReferenceFileNames,
        params DiagnosticResult[] expected)
    {
        var test = CreateTest(source);

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            var references = project.MetadataReferences
                .Select(reference => reference is PortableExecutableReference { FilePath: { } filePath } peReference
                    && aliasedReferenceFileNames.Contains(Path.GetFileName(filePath), StringComparer.OrdinalIgnoreCase)
                        ? peReference.WithAliases([alias])
                        : reference)
                .ToList();

            if (!references.Any(reference => reference.Properties.Aliases.Contains(alias)))
            {
                throw new InvalidOperationException($"None of the references matched {string.Join(", ", aliasedReferenceFileNames)}.");
            }

            return solution.WithProjectMetadataReferences(projectId, references);
        });

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    /// <summary>
    /// Like <see cref="VerifyAnalyzerAsync(string, DiagnosticResult[])"/>, but also references a second assembly,
    /// compiled from <paramref name="additionalAssemblySource"/> and only reachable through <c>extern alias</c>
    /// <paramref name="additionalAssemblyAlias"/>. When <paramref name="assertionsAlias"/> is set, TUnit.Assertions
    /// itself is also only reachable through that alias.
    /// </summary>
    public static async Task VerifyAnalyzerWithAdditionalAliasedAssemblyAsync(
        [StringSyntax("c#-test")] string source,
        [StringSyntax("c#-test")] string additionalAssemblySource,
        string additionalAssemblyAlias,
        string? assertionsAlias,
        params DiagnosticResult[] expected)
    {
        var test = CreateTest(source);

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            var references = project.MetadataReferences
                .Select(reference => assertionsAlias is not null
                    && reference is PortableExecutableReference { FilePath: { } filePath } peReference
                    && string.Equals(Path.GetFileName(filePath), "TUnit.Assertions.netstandard2.0.dll", StringComparison.OrdinalIgnoreCase)
                        ? peReference.WithAliases([assertionsAlias])
                        : reference)
                .ToList();

            var additionalCompilation = CSharpCompilation.Create(
                "TUnit.Assertions.AdditionalCopy",
                [CSharpSyntaxTree.ParseText(additionalAssemblySource)],
                project.MetadataReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

            using var image = new MemoryStream();
            var emitResult = additionalCompilation.Emit(image);

            if (!emitResult.Success)
            {
                throw new InvalidOperationException(
                    "Additional assembly failed to compile: " + string.Join(Environment.NewLine, emitResult.Diagnostics));
            }

            references.Add(MetadataReference.CreateFromImage(image.ToArray()).WithAliases([additionalAssemblyAlias]));

            return solution.WithProjectMetadataReferences(projectId, references);
        });

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    private static Test CreateTest(string source)
    {
        return new Test
        {
            TestCode = source,
            ReferenceAssemblies = GetReferenceAssemblies()
                .AddPackages([new PackageIdentity("xunit.v3.assert", "2.0.0")]),
            TestState =
            {
                AdditionalReferences =
                {
                    TUnit.Tests.Shared.AnalyzerTestCompatibility.GetCompatibleDllPath("TUnit.Core", typeof(TUnitAttribute).Assembly),
                    TUnit.Tests.Shared.AnalyzerTestCompatibility.GetCompatibleDllPath("TUnit.Assertions", typeof(Assert).Assembly),
                    TUnit.Tests.Shared.AnalyzerTestCompatibility.GetCompatibleDllPath("TUnit.Assertions.Should", typeof(TUnit.Assertions.Should.ShouldExtensions).Assembly),
#if NET8_0
                    TUnit.Tests.Shared.AnalyzerTestCompatibility.GetSystemTextJson9DllPath(),
#endif
                },
            },
            CompilerDiagnostics = CompilerDiagnostics.None
        };
    }
}
