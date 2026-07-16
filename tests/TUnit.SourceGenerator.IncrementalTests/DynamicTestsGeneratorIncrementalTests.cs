using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

public class DynamicTestsGeneratorIncrementalTests
{
    private const string DefaultConverter =
        """
        using TUnit.TestProject.Attributes;
        using TUnit.Core;

        #nullable enabled
        public class DynamicTestArgumentsTests
        {
            [DynamicTestBuilder]
            public void BuildDynamicTests(DynamicTestBuilderContext context)
            {
            }

            public void SecondBuildDynamicTests(DynamicTestBuilderContext context)
            {
            }
        }
        """;

    [Fact]
    public void AddUnrelatedType_MethodShouldNotRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultConverter, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<DynamicTestsGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    [Fact]
    public void ModifyDynamicMethodShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultConverter, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<DynamicTestsGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New, 0);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "BuildDynamicTests",
            """
            [DynamicTestBuilder]
            public static void BuildDynamicTests(DynamicTestBuilderContext context)
            {
            }
            """
        );

        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified);
    }

    [Fact]
    public void AddDynamicMethodShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultConverter, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<DynamicTestsGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "SecondBuildDynamicTests",
            """
            [DynamicTestBuilder]
            public void SecondBuildDynamicTests(DynamicTestBuilderContext context)
            {
            }
            """
            );

        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached, 0);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.New, 1);
    }

    private static void AssertRunReasons(
        GeneratorDriver driver,
        IncrementalGeneratorRunReasons reasons,
        int outputIndex = 0
    )
    {
        var runResult = driver.GetRunResult().Results[0];

        TestHelper.AssertRunReason(runResult, DynamicTestsGenerator.ParseDynamicTests, reasons.BuildStep, outputIndex);
    }
}
