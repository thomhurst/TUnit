using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.SourceGenerator.IncrementalTests;

public class StaticPropertyInitializationGeneratorIncrementalTests
{
    private const string DefaultProperties =
        """
        #nullable enabled
        using System.ComponentModel;
        using TUnit.Assertions.Attributes;
        using TUnit.Core;

        public class StaticPropertyDataSourceTests
        {
            // Static property with Arguments attribute
            [Arguments("static injected value")]
            public static string? StaticStringProperty { get; set; }

            // Static property with MethodDataSource
            [MethodDataSource(nameof(GetStaticTestData))]
            public static string? StaticDataProperty { get; set; }

            public static IStaticTestDataProvider? StaticDataProviderProperty { get; set; }
        }
        """;

    [Test]
    public async Task AddUnrelatedMethodShouldNotRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultProperties, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<StaticPropertyInitializationGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunParseLength(driver2,2);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    [Test]
    public async Task AddUnrelatedClassWithBaseListShouldNotRerunParse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultProperties, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<StaticPropertyInitializationGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // A class with a base list is a syntactic candidate, but its chain has no static
        // data-source property, so it must not reach the collected input of the parse step.
        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            """
            public class Unrelated : System.IDisposable
            {
                public void Dispose() { }
            }
            """));
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunParseLength(driver2, 2);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    [Test]
    public async Task AddClassWithValidPropertyShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultProperties, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<StaticPropertyInitializationGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            """
            using TUnit.Assertions.Attributes;
            using TUnit.Core;

            public class ExtraStaticProperty
            {
                // Static property with ClassDataSource
                [ClassDataSource<StaticTestDataProvider>]
                public static IStaticTestDataProvider? StaticDataProviderProperty { get; set; }
            }
            """));
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunParseLength(driver2,3);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    [Test]
    public async Task ModifyPropertyAttributeShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultProperties, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<StaticPropertyInitializationGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            """
            using TUnit.Assertions.Attributes;
            using TUnit.Core;

            public class ExtraStaticProperty
            {
                // Static property with ClassDataSource
                [ClassDataSource<StaticTestDataProvider>]
                public static IStaticTestDataProvider? StaticDataProviderProperty { get; set; }
            }
            """));
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task HiddenBaseStaticPropertyIsInitializedRegardlessOfDeclarationOrder(bool derivedFirst)
    {
        const string baseClass =
            """
            using TUnit.Core;

            public class HidingBase
            {
                [Arguments("base")]
                public static string? Value { get; set; }
            }
            """;

        const string derivedClass =
            """
            using TUnit.Core;

            public class HidingDerived : HidingBase
            {
                [Arguments("derived")]
                public new static string? Value { get; set; }
            }
            """;

        var baseTree = CSharpSyntaxTree.ParseText(baseClass, CSharpParseOptions.Default);
        var derivedTree = CSharpSyntaxTree.ParseText(derivedClass, CSharpParseOptions.Default);
        var compilation = derivedFirst
            ? Fixture.CreateLibrary(derivedTree, baseTree)
            : Fixture.CreateLibrary(baseTree, derivedTree);

        var driver = TestHelper.GenerateTracked<StaticPropertyInitializationGenerator>(compilation);

        // Both HidingDerived.Value and the hidden HidingBase.Value need initializing
        await AssertRunParseLength(driver, 2);
    }

    private static async Task AssertRunReasons(
        GeneratorDriver driver,
        IncrementalGeneratorRunReasons reasons,
        int outputIndex = 0
    )
    {
        var runResult = driver.GetRunResult().Results[0];
        await TestHelper.AssertRunReason(runResult, StaticPropertyInitializationGenerator.ParseStaticProperties, reasons.BuildStep, outputIndex);
    }

    private static async Task AssertRunParseLength(
        GeneratorDriver driver,
        int staticPropertyModelCount
    )
    {
        var runResult = driver.GetRunResult().Results[0];
        var runValue = runResult.TrackedSteps[StaticPropertyInitializationGenerator.ParseStaticProperties][0].Outputs[0].Value;
        var runState = (EquatableArray<PropertyWithDataSourceModel>)runValue;
        await Assert.That(runState.Length).IsEqualTo(staticPropertyModelCount);
    }
}