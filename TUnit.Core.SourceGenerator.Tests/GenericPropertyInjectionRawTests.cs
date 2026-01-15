using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;
using TUnit.Core.SourceGenerator.Tests.Extensions;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Raw tests to verify what the PropertyInjectionSourceGenerator produces for generic types.
/// These tests don't use Verify, just direct assertions.
/// </summary>
internal class GenericPropertyInjectionRawTests
{
    /// <summary>
    /// Helper to run the generator and return generated files.
    /// </summary>
    private static async Task<string[]> RunGeneratorAsync(string source)
    {
        var generator = new PropertyInjectionSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
                "TestAssembly",
                [CSharpSyntaxTree.ParseText(source)],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            )
            .WithReferences(ReferencesHelper.References);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        await Assert.That(errors).IsEmpty()
            .Because($"Generator errors: {string.Join("\n", errors.Select(e => e.GetMessage()))}");

        return newCompilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .Where(t => !t.Contains("namespace TestProject"))
            .ToArray();
    }

    [Test]
    public async Task BasicGenericBase_WithDataSourceProperty_GeneratesMetadata()
    {
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;

            namespace TestProject;

            public abstract class GenericFixtureBase<TProgram> where TProgram : class
            {
                [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
                public InMemoryDatabase? Database { get; init; }
            }

            public class MyTests : GenericFixtureBase<MyTests.TestProgram>
            {
                public class TestProgram { }

                [Test]
                public Task MyTest() => Task.CompletedTask;
            }

            public class InMemoryDatabase : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        var hasGenericMetadata = generatedFiles.Any(f =>
            f.Contains("GenericFixtureBase") &&
            f.Contains("PropertySourceRegistry.Register"));

        await Assert.That(hasGenericMetadata)
            .IsTrue()
            .Because("Should generate property source for GenericFixtureBase<TestProgram>");
    }

    [Test]
    public async Task MultiplePropertiesOnGenericBase_AllPropertiesGenerated()
    {
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;

            namespace TestProject;

            public abstract class GenericFixtureBase<TProgram> where TProgram : class
            {
                [ClassDataSource<Database1>(Shared = SharedType.PerTestSession)]
                public Database1? FirstDb { get; init; }

                [ClassDataSource<Database2>(Shared = SharedType.PerTestSession)]
                public Database2? SecondDb { get; init; }
            }

            public class MyTests : GenericFixtureBase<MyTests.TestProgram>
            {
                public class TestProgram { }

                [Test]
                public Task MyTest() => Task.CompletedTask;
            }

            public class Database1 : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }

            public class Database2 : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        // Should have property injection for both properties
        var genericPropertyFile = generatedFiles.FirstOrDefault(f =>
            f.Contains("GenericFixtureBase") &&
            f.Contains("PropertySourceRegistry.Register"));

        await Assert.That(genericPropertyFile).IsNotNull()
            .Because("Should generate property source for GenericFixtureBase");

        // Both properties should be registered
        await Assert.That(genericPropertyFile!.Contains("FirstDb")).IsTrue()
            .Because("FirstDb property should be registered");
        await Assert.That(genericPropertyFile.Contains("SecondDb")).IsTrue()
            .Because("SecondDb property should be registered");
    }

    [Test]
    public async Task MultipleTypeParameters_GeneratesCorrectMetadata()
    {
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;

            namespace TestProject;

            public abstract class GenericFixtureBase<T1, T2> where T1 : class where T2 : class
            {
                [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
                public InMemoryDatabase? Database { get; init; }
            }

            public class MyTests : GenericFixtureBase<MyTests.Program1, MyTests.Program2>
            {
                public class Program1 { }
                public class Program2 { }

                [Test]
                public Task MyTest() => Task.CompletedTask;
            }

            public class InMemoryDatabase : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        var hasGenericMetadata = generatedFiles.Any(f =>
            f.Contains("GenericFixtureBase") &&
            f.Contains("Program1") &&
            f.Contains("Program2") &&
            f.Contains("PropertySourceRegistry.Register"));

        await Assert.That(hasGenericMetadata)
            .IsTrue()
            .Because("Should generate property source for GenericFixtureBase<Program1, Program2>");
    }

    [Test]
    public async Task DeepInheritanceChain_GeneratesMetadataForAllLevels()
    {
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;

            namespace TestProject;

            public abstract class GrandparentBase<T> where T : class
            {
                [ClassDataSource<GrandparentDb>(Shared = SharedType.PerTestSession)]
                public GrandparentDb? GrandparentDatabase { get; init; }
            }

            public abstract class ParentBase<T> : GrandparentBase<T> where T : class
            {
                [ClassDataSource<ParentDb>(Shared = SharedType.PerTestSession)]
                public ParentDb? ParentDatabase { get; init; }
            }

            public class MyTests : ParentBase<MyTests.TestProgram>
            {
                public class TestProgram { }

                [Test]
                public Task MyTest() => Task.CompletedTask;
            }

            public class GrandparentDb : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }

            public class ParentDb : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        // Should have metadata for both GrandparentBase and ParentBase
        var hasGrandparentMetadata = generatedFiles.Any(f =>
            f.Contains("GrandparentBase") &&
            f.Contains("PropertySourceRegistry.Register"));

        var hasParentMetadata = generatedFiles.Any(f =>
            f.Contains("ParentBase") &&
            f.Contains("PropertySourceRegistry.Register"));

        await Assert.That(hasGrandparentMetadata)
            .IsTrue()
            .Because("Should generate property source for GrandparentBase<TestProgram>");

        await Assert.That(hasParentMetadata)
            .IsTrue()
            .Because("Should generate property source for ParentBase<TestProgram>");
    }

    [Test]
    public async Task GenericTypeAsDataSourceTypeArgument_GeneratesPropertySource()
    {
        // This test verifies that a generic IAsyncInitializer discovered from ClassDataSource
        // type argument generates PropertySourceRegistry for the test class (MyTests).
        // Note: InitializerPropertyRegistry is only generated when the type has properties
        // returning other IAsyncInitializer types.
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;

            namespace TestProject;

            public class GenericFixture<T> : IAsyncInitializer where T : class
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }

            public class MyTests
            {
                [ClassDataSource<GenericFixture<MyTests>>(Shared = SharedType.PerTestSession)]
                public GenericFixture<MyTests>? Fixture { get; init; }

                [Test]
                public Task MyTest() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        // Should generate PropertySourceRegistry for MyTests (which has the ClassDataSource property)
        var hasPropertySourceMetadata = generatedFiles.Any(f =>
            f.Contains("MyTests") &&
            f.Contains("PropertySourceRegistry.Register"));

        await Assert.That(hasPropertySourceMetadata)
            .IsTrue()
            .Because("Should generate property source for MyTests with generic ClassDataSource type");
    }

    [Test]
    public async Task GenericIAsyncInitializerWithNestedProperties_GeneratesMetadata()
    {
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;

            namespace TestProject;

            public class GenericFixture<T> : IAsyncInitializer where T : class
            {
                public NestedInitializer? Nested { get; init; }
                public Task InitializeAsync() => Task.CompletedTask;
            }

            public class NestedInitializer : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }

            public class MyTests
            {
                [ClassDataSource<GenericFixture<MyTests>>(Shared = SharedType.PerTestSession)]
                public GenericFixture<MyTests>? Fixture { get; init; }

                [Test]
                public Task MyTest() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        // Should generate InitializerPropertyRegistry with the Nested property
        var genericInitializerFile = generatedFiles.FirstOrDefault(f =>
            f.Contains("GenericFixture") &&
            f.Contains("InitializerPropertyRegistry.Register"));

        await Assert.That(genericInitializerFile).IsNotNull()
            .Because("Should generate initializer property source for GenericFixture<MyTests>");

        await Assert.That(genericInitializerFile!.Contains("Nested")).IsTrue()
            .Because("Nested property should be discovered in GenericFixture");
    }

    [Test]
    public async Task MultipleConcreteInstantiationsOfSameGeneric_GeneratesDistinctMetadata()
    {
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;

            namespace TestProject;

            public abstract class GenericFixtureBase<TProgram> where TProgram : class
            {
                [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
                public InMemoryDatabase? Database { get; init; }
            }

            public class Tests1 : GenericFixtureBase<Tests1.Program1>
            {
                public class Program1 { }

                [Test]
                public Task MyTest() => Task.CompletedTask;
            }

            public class Tests2 : GenericFixtureBase<Tests2.Program2>
            {
                public class Program2 { }

                [Test]
                public Task MyTest() => Task.CompletedTask;
            }

            public class InMemoryDatabase : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        // Should have metadata for both concrete instantiations
        var hasProgram1Metadata = generatedFiles.Any(f =>
            f.Contains("GenericFixtureBase") &&
            f.Contains("Program1") &&
            f.Contains("PropertySourceRegistry.Register"));

        var hasProgram2Metadata = generatedFiles.Any(f =>
            f.Contains("GenericFixtureBase") &&
            f.Contains("Program2") &&
            f.Contains("PropertySourceRegistry.Register"));

        await Assert.That(hasProgram1Metadata)
            .IsTrue()
            .Because("Should generate property source for GenericFixtureBase<Program1>");

        await Assert.That(hasProgram2Metadata)
            .IsTrue()
            .Because("Should generate property source for GenericFixtureBase<Program2>");
    }

    [Test]
    public async Task MixOfGenericAndNonGenericProperties_BothGenerated()
    {
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;

            namespace TestProject;

            public abstract class GenericFixtureBase<TProgram> where TProgram : class
            {
                [ClassDataSource<Database1>(Shared = SharedType.PerTestSession)]
                public Database1? BaseDatabase { get; init; }
            }

            public class MyTests : GenericFixtureBase<MyTests.TestProgram>
            {
                public class TestProgram { }

                [ClassDataSource<Database2>(Shared = SharedType.PerTestSession)]
                public Database2? DerivedDatabase { get; init; }

                [Test]
                public Task MyTest() => Task.CompletedTask;
            }

            public class Database1 : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }

            public class Database2 : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        // Should have property injection for the generic base type
        var hasGenericBaseMetadata = generatedFiles.Any(f =>
            f.Contains("GenericFixtureBase") &&
            f.Contains("BaseDatabase") &&
            f.Contains("PropertySourceRegistry.Register"));

        // Should have property injection for the concrete derived type
        var hasDerivedMetadata = generatedFiles.Any(f =>
            f.Contains("MyTests") &&
            f.Contains("DerivedDatabase") &&
            f.Contains("PropertySourceRegistry.Register"));

        await Assert.That(hasGenericBaseMetadata)
            .IsTrue()
            .Because("Should generate property source for generic base class");

        await Assert.That(hasDerivedMetadata)
            .IsTrue()
            .Because("Should generate property source for derived class");
    }

    [Test]
    public async Task NestedGenericTypeArgument_GeneratesMetadata()
    {
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;
            using System.Collections.Generic;

            namespace TestProject;

            public abstract class GenericFixtureBase<TProgram> where TProgram : class
            {
                [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
                public InMemoryDatabase? Database { get; init; }
            }

            public class MyTests : GenericFixtureBase<List<string>>
            {
                [Test]
                public Task MyTest() => Task.CompletedTask;
            }

            public class InMemoryDatabase : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        // Should generate metadata for GenericFixtureBase<List<string>>
        var hasGenericMetadata = generatedFiles.Any(f =>
            f.Contains("GenericFixtureBase") &&
            f.Contains("PropertySourceRegistry.Register"));

        await Assert.That(hasGenericMetadata)
            .IsTrue()
            .Because("Should generate property source for GenericFixtureBase<List<string>>");
    }

    [Test]
    public async Task OpenGenericIntermediateClass_ConcreteAtLeaf_GeneratesMetadata()
    {
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;

            namespace TestProject;

            public abstract class GenericBase<T> where T : class
            {
                [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
                public InMemoryDatabase? Database { get; init; }
            }

            // Intermediate class keeps the type parameter open
            public abstract class IntermediateBase<T> : GenericBase<T> where T : class
            {
            }

            // Leaf class makes it concrete
            public class MyTests : IntermediateBase<MyTests.TestProgram>
            {
                public class TestProgram { }

                [Test]
                public Task MyTest() => Task.CompletedTask;
            }

            public class InMemoryDatabase : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        // Should generate metadata for GenericBase<TestProgram>
        var hasGenericMetadata = generatedFiles.Any(f =>
            f.Contains("GenericBase") &&
            f.Contains("TestProgram") &&
            f.Contains("PropertySourceRegistry.Register"));

        await Assert.That(hasGenericMetadata)
            .IsTrue()
            .Because("Should generate property source for GenericBase<TestProgram> through intermediate class");
    }

    /// <summary>
    /// Issue #4431 - Tests the exact WebApplicationFactory pattern from the GitHub issue.
    /// A generic factory class with ClassDataSource property, used via ClassDataSource itself.
    /// </summary>
    [Test]
    public async Task Issue4431_WebApplicationFactoryPattern_GeneratesMetadata()
    {
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;

            namespace TestProject;

            // Generic WebApplicationFactory-style class with its own ClassDataSource dependency
            public class CustomWebApplicationFactory<TProgram> : IAsyncInitializer
                where TProgram : class
            {
                // This property needs to be discovered and injected
                [ClassDataSource<TestContainer>(Shared = SharedType.PerTestSession)]
                public TestContainer? Container { get; init; }

                public Task InitializeAsync() => Task.CompletedTask;
            }

            public class TestContainer : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }

            public class MyProgram { }

            // Test class using the factory
            public class MyTests
            {
                [ClassDataSource<CustomWebApplicationFactory<MyProgram>>(Shared = SharedType.PerTestSession)]
                public CustomWebApplicationFactory<MyProgram>? Factory { get; init; }

                [Test]
                public Task MyTest() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        // Should generate PropertySourceRegistry for CustomWebApplicationFactory<MyProgram>
        var hasFactoryPropertySource = generatedFiles.Any(f =>
            f.Contains("CustomWebApplicationFactory") &&
            f.Contains("MyProgram") &&
            f.Contains("PropertySourceRegistry.Register"));

        // Should also generate InitializerPropertyRegistry for the nested Container property
        var hasFactoryInitializerRegistry = generatedFiles.Any(f =>
            f.Contains("CustomWebApplicationFactory") &&
            f.Contains("InitializerPropertyRegistry.Register"));

        await Assert.That(hasFactoryPropertySource)
            .IsTrue()
            .Because("Should generate property source for CustomWebApplicationFactory<MyProgram>");

        await Assert.That(hasFactoryInitializerRegistry)
            .IsTrue()
            .Because("Should generate initializer property registry for CustomWebApplicationFactory with Container property");
    }

    /// <summary>
    /// Issue #4431 Comment - Tests the multi-parameter generic base inheritance pattern.
    /// </summary>
    [Test]
    public async Task Issue4431_MultiParameterGenericBase_GeneratesMetadata()
    {
        var source = """
            using TUnit.Core;
            using TUnit.Core.Interfaces;

            namespace TestProject;

            public class CustomFactory<T> : IAsyncInitializer where T : class
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }

            // Base class with multiple type parameters
            public abstract class WebAppFactoryBase<TFactory, TProgram>
                where TFactory : CustomFactory<TProgram>
                where TProgram : class
            {
                [ClassDataSource<TestDatabase>(Shared = SharedType.PerTestSession)]
                public TestDatabase? Database { get; init; }
            }

            public class TestDatabase : IAsyncInitializer
            {
                public Task InitializeAsync() => Task.CompletedTask;
            }

            public class MyProgram { }

            // Concrete test class
            public class MyTests : WebAppFactoryBase<CustomFactory<MyProgram>, MyProgram>
            {
                [Test]
                public Task MyTest() => Task.CompletedTask;
            }
            """;

        var generatedFiles = await RunGeneratorAsync(source);

        // Should generate PropertySourceRegistry for WebAppFactoryBase with concrete type args
        var hasGenericBaseMetadata = generatedFiles.Any(f =>
            f.Contains("WebAppFactoryBase") &&
            f.Contains("PropertySourceRegistry.Register"));

        await Assert.That(hasGenericBaseMetadata)
            .IsTrue()
            .Because("Should generate property source for WebAppFactoryBase<CustomFactory<MyProgram>, MyProgram>");
    }
}
