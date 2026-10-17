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

    [Test]
    public async Task AddUnrelatedType_MethodShouldNotRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultConverter, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<DynamicTestsGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    [Test]
    public async Task ModifyDynamicMethodShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultConverter, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<DynamicTestsGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New, 0);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "BuildDynamicTests",
            """
            [DynamicTestBuilder]
            public static void BuildDynamicTests(DynamicTestBuilderContext context)
            {
            }
            """
        );

        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified);
    }

    [Test]
    public async Task AddDynamicMethodShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultConverter, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<DynamicTestsGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "SecondBuildDynamicTests",
            """
            [DynamicTestBuilder]
            public void SecondBuildDynamicTests(DynamicTestBuilderContext context)
            {
            }
            """
            );

        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached, 0);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.New, 1);
    }

    [Test]
    public async Task MoveDynamicMethodToDifferentLine_ShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultConverter, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<DynamicTestsGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // The [CallerLineNumber] captured by DynamicTestBuilder feeds the hint name and the
        // generated DynamicTestBuilderContext, so shifting the method must regenerate.
        var compilation2 = compilation1.ReplaceSyntaxTree(
            syntaxTree,
            CSharpSyntaxTree.ParseText(DefaultConverter.Replace("public class DynamicTestArgumentsTests", "\n\npublic class DynamicTestArgumentsTests"), CSharpParseOptions.Default));

        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified);
    }

    private static async Task AssertRunReasons(
        GeneratorDriver driver,
        IncrementalGeneratorRunReasons reasons,
        int outputIndex = 0
    )
    {
        var runResult = driver.GetRunResult().Results[0];

        await TestHelper.AssertRunReason(runResult, DynamicTestsGenerator.ParseDynamicTests, reasons.BuildStep, outputIndex);
    }
}