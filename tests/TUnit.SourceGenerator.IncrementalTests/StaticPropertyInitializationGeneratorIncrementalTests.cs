using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

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

    [Fact]
    public void AddUnrelatedMethodShouldNotRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultProperties, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<StaticPropertyInitializationGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunParseLength(driver2,2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Unchanged);
    }

    [Fact]
    public void AddClassWithValidPropertyShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultProperties, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<StaticPropertyInitializationGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

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
        AssertRunParseLength(driver2,3);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    [Fact]
    public void ModifyPropertyAttributeShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultProperties, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<StaticPropertyInitializationGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

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
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified);
    }

    private static void AssertRunReasons(
        GeneratorDriver driver,
        IncrementalGeneratorRunReasons reasons,
        int outputIndex = 0
    )
    {
        var runResult = driver.GetRunResult().Results[0];
        TestHelper.AssertRunReason(runResult, StaticPropertyInitializationGenerator.ParseStaticProperties, reasons.BuildStep, outputIndex);
    }

    private static void AssertRunParseLength(
        GeneratorDriver driver,
        int staticPropertyModelCount
    )
    {
        var runResult = driver.GetRunResult().Results[0];
        var runValue = runResult.TrackedSteps[StaticPropertyInitializationGenerator.ParseStaticProperties][0].Outputs[0].Value;
        var runState = (EquatableArray<PropertyWithDataSourceModel>)runValue;
        Xunit.Assert.Equal(staticPropertyModelCount, runState.Length);
    }
}
