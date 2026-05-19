using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Core.SourceGenerator.Tests;

internal class ExcludeFromTestDiscoveryTests
{
    private const string ExcludedAssemblySource = """
        using System.Threading.Tasks;
        using TUnit.Core;

        [assembly: ExcludeFromTestDiscovery]

        namespace TestProject;

        public class ExcludedTests
        {
            [Test]
            public Task Test() => Task.CompletedTask;

            [Before(HookType.Test)]
            public static void BeforeTest()
            {
            }

            [DynamicTestBuilder]
            public static void Build(DynamicTestBuilderContext context)
            {
            }
        }
        """;

    [Test]
    public async Task TestMetadataGenerator_DoesNotGenerateTests_ForExcludedAssembly()
    {
        var generatedFiles = await RunGeneratorAsync<TestMetadataGenerator>(ExcludedAssemblySource);

        await Assert.That(generatedFiles).IsEmpty();
    }

    [Test]
    public async Task HookMetadataGenerator_DoesNotGenerateHooks_ForExcludedAssembly()
    {
        var generatedFiles = await RunGeneratorAsync<HookMetadataGenerator>(ExcludedAssemblySource);

        await Assert.That(generatedFiles).IsEmpty();
    }

    [Test]
    public async Task DynamicTestsGenerator_DoesNotGenerateDynamicTests_ForExcludedAssembly()
    {
        var generatedFiles = await RunGeneratorAsync<DynamicTestsGenerator>(ExcludedAssemblySource);

        await Assert.That(generatedFiles).IsEmpty();
    }

    [Test]
    public async Task InfrastructureGenerator_DoesNotGenerateInfrastructure_ForExcludedAssembly()
    {
        var generatedFiles = await RunGeneratorAsync<InfrastructureGenerator>(ExcludedAssemblySource);

        await Assert.That(generatedFiles).IsEmpty();
    }

    private static async Task<string[]> RunGeneratorAsync<TGenerator>(string source)
        where TGenerator : IIncrementalGenerator, new()
    {
        var generator = new TGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
                "TestAssembly",
                [CSharpSyntaxTree.ParseText(source)],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            )
            .WithReferences(ReferencesHelper.References);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        var errors = diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
        await Assert.That(errors).IsEmpty()
            .Because($"Generator errors: {string.Join("\n", errors.Select(static e => e.GetMessage()))}");

        return newCompilation.SyntaxTrees
            .Select(static tree => tree.GetText().ToString())
            .Where(text => text != source)
            .ToArray();
    }
}
