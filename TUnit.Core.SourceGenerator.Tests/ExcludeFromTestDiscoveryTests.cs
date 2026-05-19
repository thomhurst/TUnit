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

    private const string EntryAssemblyExclusionSource = """
        using System.Threading.Tasks;
        using ExternalTests;
        using TUnit.Core;

        [assembly: ExcludeFromTestDiscovery(typeof(Marker))]

        namespace TestProject;

        public class EntryTests
        {
            [Test]
            public Task Test() => Task.CompletedTask;
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

    [Test]
    public async Task TestMetadataGenerator_GeneratesEntryAssemblyTests_WhenReferencedAssemblyIsExcluded()
    {
        var externalReference = CreateReference("ExternalTests", "namespace ExternalTests; public class Marker { }");

        var generatedFiles = await RunGeneratorAsync<TestMetadataGenerator>(
            EntryAssemblyExclusionSource,
            [externalReference]);

        await Assert.That(generatedFiles).IsNotEmpty();
        await Assert.That(generatedFiles.Any(static file => file.Contains("EntryTests"))).IsTrue();
    }

    [Test]
    public async Task InfrastructureGenerator_RegistersExcludedAssemblyName_WithoutReferencingMarkerType()
    {
        var externalReference = CreateReference("ExternalTests", "namespace ExternalTests; public class Marker { }");

        var generatedFiles = await RunGeneratorAsync<InfrastructureGenerator>(
            EntryAssemblyExclusionSource,
            [externalReference]);

        var infrastructureFile = generatedFiles.Single(static file => file.Contains("TUnitInfrastructure"));

        await Assert.That(infrastructureFile).Contains("""ExcludeAssemblyFromDiscovery("ExternalTests")""");
        await Assert.That(infrastructureFile).DoesNotContain("typeof(global::ExternalTests.Marker)");
    }

    private static async Task<string[]> RunGeneratorAsync<TGenerator>(string source)
        where TGenerator : IIncrementalGenerator, new()
    {
        return await RunGeneratorAsync<TGenerator>(source, []);
    }

    private static async Task<string[]> RunGeneratorAsync<TGenerator>(
        string source,
        PortableExecutableReference[] additionalReferences)
        where TGenerator : IIncrementalGenerator, new()
    {
        var generator = new TGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
                "TestAssembly",
                [CSharpSyntaxTree.ParseText(source)],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            )
            .WithReferences(ReferencesHelper.References)
            .AddReferences(additionalReferences);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        var errors = diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
        await Assert.That(errors).IsEmpty()
            .Because($"Generator errors: {string.Join("\n", errors.Select(static e => e.GetMessage()))}");

        return newCompilation.SyntaxTrees
            .Select(static tree => tree.GetText().ToString())
            .Where(text => text != source)
            .ToArray();
    }

    private static PortableExecutableReference CreateReference(string assemblyName, string source)
    {
        var compilation = CSharpCompilation.Create(
                assemblyName,
                [CSharpSyntaxTree.ParseText(source)],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            )
            .WithReferences(ReferencesHelper.References);

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Failed to create test reference: {string.Join(Environment.NewLine, result.Diagnostics)}");
        }

        return MetadataReference.CreateFromImage(stream.ToArray());
    }
}
