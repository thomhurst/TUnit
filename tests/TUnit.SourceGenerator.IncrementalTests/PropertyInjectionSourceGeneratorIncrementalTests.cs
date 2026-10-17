using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

public class PropertyInjectionSourceGeneratorIncrementalTests
{
    private const string DefaultSource =
        """
        #nullable enable
        using System.Threading.Tasks;
        using TUnit.Core;
        using TUnit.Core.Interfaces;

        public class Dependency : IAsyncInitializer
        {
            public Task InitializeAsync() => Task.CompletedTask;
        }

        public class Fixture : IAsyncInitializer
        {
            public Dependency Inner { get; } = new();

            public Task InitializeAsync() => Task.CompletedTask;
        }

        public class GenericBase<T>
        {
            [ClassDataSource<Dependency>]
            public Dependency? Shared { get; set; }
        }

        public class Tests : GenericBase<int>
        {
            [ClassDataSource<Fixture>]
            public Fixture? Injected { get; set; }

            public int Plain { get; set; }

            [Test]
            public void Test1()
            {
            }
        }
        """;

    private static readonly string[] Steps =
    [
        PropertyInjectionSourceGenerator.PropertyDataSourcesStep,
        PropertyInjectionSourceGenerator.AsyncInitializersStep,
        PropertyInjectionSourceGenerator.ConcreteGenericTypesStep,
    ];

    [Test]
    public async Task AddUnrelatedTypeShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<PropertyInjectionSourceGenerator>(compilation1);
        await AssertAllOutputs(driver1, IncrementalStepRunReason.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            """
            public struct MyValue { }

            public class Unrelated
            {
                public int Number { get; set; }
                public string? Text { get; set; }
                public (int, string) Tuple { get; set; }
            }
            """));
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertAllOutputs(driver2, IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
    }

    [Test]
    public async Task EditUnrelatedMemberShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<PropertyInjectionSourceGenerator>(compilation1);
        await AssertAllOutputs(driver1, IncrementalStepRunReason.New);

        var compilation2 = TestHelper.ReplacePropertyDeclaration(compilation1, "Plain", "public long Plain { get; set; }");
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertAllOutputs(driver2, IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
    }

    [Test]
    public async Task EditUnrelatedMemberShouldNotRerunConcreteGenericTypes()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<PropertyInjectionSourceGenerator>(compilation1);

        // The per-node transform output must be equatable, so an unchanged result stops the
        // flattened step from re-running rather than merely re-producing equal values.
        var compilation2 = TestHelper.ReplacePropertyDeclaration(compilation1, "Plain", "public long Plain { get; set; }");
        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertAllRunReasons(driver2.GetRunResult().Results[0],
            PropertyInjectionSourceGenerator.ConcreteGenericTypesStep, IncrementalStepRunReason.Cached);
    }

    [Test]
    public async Task ModifyDataSourcePropertyShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<PropertyInjectionSourceGenerator>(compilation1);

        var compilation2 = TestHelper.ReplacePropertyDeclaration(compilation1, "Injected",
            """
            [ClassDataSource<Fixture>(Shared = SharedType.PerTestSession)]
            public Fixture? Injected { get; set; }
            """);
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertAnyOutput(driver2, PropertyInjectionSourceGenerator.PropertyDataSourcesStep, IncrementalStepRunReason.Modified);
    }

    [Test]
    public async Task AddInitializerPropertyShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<PropertyInjectionSourceGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceTypeDeclaration(compilation1, "Fixture",
            """
            public class Fixture : IAsyncInitializer
            {
                public Dependency Inner { get; } = new();
                public Dependency Second { get; } = new();

                public Task InitializeAsync() => Task.CompletedTask;
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertAnyOutput(driver2, PropertyInjectionSourceGenerator.AsyncInitializersStep, IncrementalStepRunReason.Modified);
    }

    [Test]
    public async Task AddConcreteGenericInstantiationShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource, CSharpParseOptions.Default));

        var driver1 = TestHelper.GenerateTracked<PropertyInjectionSourceGenerator>(compilation1);
        var concreteTypes1 = driver1.GetRunResult().Results[0].TrackedSteps[PropertyInjectionSourceGenerator.ConcreteGenericTypesStep]
            .SelectMany(x => x.Outputs)
            .Count();

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            """
            using TUnit.Core;

            public class OtherTests : GenericBase<string>
            {
                [Test]
                public void Test1()
                {
                }
            }
            """));
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertAnyOutput(driver2, PropertyInjectionSourceGenerator.ConcreteGenericTypesStep, IncrementalStepRunReason.New);

        var concreteTypes2 = driver2.GetRunResult().Results[0].TrackedSteps[PropertyInjectionSourceGenerator.ConcreteGenericTypesStep]
            .SelectMany(x => x.Outputs)
            .Count();
        await Assert.That(concreteTypes2).IsEqualTo(concreteTypes1 + 1);
    }

    private static async Task AssertAllOutputs(GeneratorDriver driver, params IncrementalStepRunReason[] allowedReasons)
    {
        var runResult = driver.GetRunResult().Results[0];

        foreach (var step in Steps)
        {
            var outputs = runResult.TrackedSteps[step].SelectMany(x => x.Outputs).ToArray();
            await Assert.That(outputs).IsNotEmpty();

            foreach (var output in outputs)
            {
                await Assert.That(allowedReasons).Contains(output.Reason)
                    .Because($"step {step} produced an output with reason {output.Reason}");
            }
        }
    }

    private static async Task AssertAnyOutput(GeneratorDriver driver, string step, IncrementalStepRunReason expectedReason)
    {
        var runResult = driver.GetRunResult().Results[0];
        var reasons = runResult.TrackedSteps[step].SelectMany(x => x.Outputs).Select(x => x.Reason).ToArray();

        await Assert.That(reasons).Contains(expectedReason)
            .Because($"step {step} should have an output with reason {expectedReason}");
    }
}