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

    [Fact]
    public void ModifyOneContainingTypeShouldOnlyRegenerateThatType()
    {
        var compilation1 = Fixture.CreateLibrary(
            DefaultAssertion,
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
            """);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);

        var longTree = compilation1.SyntaxTrees.Last();
        var compilation2 = compilation1.ReplaceSyntaxTree(longTree, CSharpSyntaxTree.ParseText(
            """
            using TUnit.Assertions.Attributes;

            public static partial class LongAssertionExtensions
            {
                [GenerateAssertion(ExpectationMessage = "to be greater than zero")]
                public static bool IsPositive(this long value)
                {
                    return value > 0;
                }
            }
            """));
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        TestHelper.AssertRunReason(runResult, MethodAssertionGenerator.BuildAssertionGroup, IncrementalStepRunReason.Unchanged, 0);
        TestHelper.AssertRunReason(runResult, MethodAssertionGenerator.BuildAssertionGroup, IncrementalStepRunReason.Modified, 1);

        var sourceOutputReasons = runResult.TrackedOutputSteps
            .SelectMany(x => x.Value)
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason)
            .ToArray();
        Xunit.Assert.Single(sourceOutputReasons, IncrementalStepRunReason.Modified);
    }

    [Fact]
    public void UnrelatedEditWithDiagnosticShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(
            DefaultAssertion,
            """
            using TUnit.Assertions.Attributes;

            public static partial class BrokenAssertionExtensions
            {
                [GenerateAssertion]
                public bool IsBroken(int value) => value > 0;
            }
            """);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);
        Xunit.Assert.Contains(driver1.GetRunResult().Diagnostics, d => d.Id == "TUNITGEN001");

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        TestHelper.AssertSourceOutputsCached(runResult);
        var diagnostic = Xunit.Assert.Single(runResult.Diagnostics, d => d.Id == "TUNITGEN001");
        Xunit.Assert.Equal(4, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
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
