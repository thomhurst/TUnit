using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

public class MethodAssertionGeneratorIncrementalTests
{
    private const string DefaultAssertion =
        """
        #nullable enabled
        using System.ComponentModel;
        using TUnit.Assertions.Attributes;

        public static partial class IntAssertionExtensions
        {
            [GenerateAssertion(ExpectationMessage = "to be positive")]
            public static bool IsPositive(this int value)
            {
                return value > 0;
            }

            public static bool IsNegative(this int value)
            {
                return value < 0;
            }
        }
        """;

    [Fact]
    public void AddUnrelatedMethodShouldNotRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultAssertion, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    [Fact]
    public void AddNewTypeAssertionShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultAssertion, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            """
            using TUnit.Assertions.Attributes;

            public static partial class LongAssertionExtensions
            {
                [GenerateAssertion(ExpectationMessage = "to be positive")]
                public static bool IsPositive(this long value)
                {
                    return value > 0;
                }
            }
            """));
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached, 0);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.New, 1);
    }

    [Fact]
    public void AddNewSameTypeAssertionShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultAssertion, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "IsNegative",
            """
            [GenerateAssertion(ExpectationMessage = "to be less than zero")]
            public static bool IsNegative(this int value)
            {
                return value < 0;
            }
            """
        );
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached, 0);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.New, 1);
    }

    [Fact]
    public void ModifyMessageShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultAssertion, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "IsPositive",
            """
            [GenerateAssertion(ExpectationMessage = "to be more than zero")]
            public static bool IsPositive(this int value)
            {
                return value > 0;
            }
            """
            );
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

        TestHelper.AssertRunReason(runResult, MethodAssertionGenerator.BuildAssertion, reasons.BuildStep, outputIndex);
    }
}
