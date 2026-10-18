using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.SourceGenerator.IncrementalTests;

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

    [Test]
    public async Task AddUnrelatedMethodShouldNotRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultAssertion, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    [Test]
    public async Task AddNewTypeAssertionShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultAssertion, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

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
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached, 0);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.New, 1);
    }

    [Test]
    public async Task AddNewSameTypeAssertionShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultAssertion, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

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
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached, 0);
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.New, 1);
    }

    [Test]
    public async Task ModifyMessageShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultAssertion, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);
        await AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

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
        await AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified);
    }

    [Test]
    public async Task ModifyOneContainingTypeShouldOnlyRegenerateThatType()
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
        await TestHelper.AssertRunReason(runResult, MethodAssertionGenerator.BuildAssertionGroup, IncrementalStepRunReason.Unchanged, 0);
        await TestHelper.AssertRunReason(runResult, MethodAssertionGenerator.BuildAssertionGroup, IncrementalStepRunReason.Modified, 1);

        var sourceOutputReasons = runResult.TrackedOutputSteps
            .SelectMany(x => x.Value)
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason)
            .ToArray();
        await Assert.That(sourceOutputReasons).HasSingleItem(x => x == IncrementalStepRunReason.Modified);
    }

    [Test]
    public async Task UnrelatedEditWithDiagnosticShouldNotRegenerate()
    {
        var brokenTree = CSharpSyntaxTree.ParseText(
            """
            using TUnit.Assertions.Attributes;

            public static partial class BrokenAssertionExtensions
            {
                [GenerateAssertion]
                public bool IsBroken(int value) => value > 0;
            }
            """,
            path: "BrokenAssertionExtensions.cs");
        var compilation1 = Fixture.CreateLibrary(
            CSharpSyntaxTree.ParseText(DefaultAssertion, path: "DefaultAssertion.cs"),
            brokenTree);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);
        await Assert.That(driver1.GetRunResult().Diagnostics).Contains(d => d.Id == "TUNITGEN001");

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        await TestHelper.AssertAllRunReasons(runResult, MethodAssertionGenerator.BuildAssertionGroup,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);

        // Only the diagnostic reporter re-runs (it is paired with the compilation); the generated file stays cached.
        var sourceOutputReasons = runResult.TrackedOutputSteps
            .SelectMany(x => x.Value)
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason)
            .ToArray();
        await Assert.That(sourceOutputReasons).HasSingleItem(reason => reason != IncrementalStepRunReason.Cached);

        var diagnostic = await Assert.That(runResult.Diagnostics).HasSingleItem(d => d.Id == "TUNITGEN001");
        await Assert.That(diagnostic.Location.GetLineSpan().StartLinePosition.Line).IsEqualTo(4);

        // The location must stay tied to the syntax tree so #pragma and per-file severities apply.
        await Assert.That(diagnostic.Location.IsInSource).IsTrue();
        await Assert.That(diagnostic.Location.SourceTree).IsSameReferenceAs(brokenTree);
    }

    [Test]
    public async Task DiagnosticInPathlessTreeKeepsSourceTree()
    {
        // Trees parsed without a path all share the empty path, so the path alone cannot identify the tree.
        var brokenTree = CSharpSyntaxTree.ParseText(
            """
            using TUnit.Assertions.Attributes;

            public static partial class BrokenAssertionExtensions
            {
                [GenerateAssertion]
                public bool IsBroken(int value) => value > 0;
            }
            """);
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultAssertion), brokenTree);

        var driver1 = TestHelper.GenerateTracked<MethodAssertionGenerator>(compilation1);
        var firstDiagnostic = await Assert.That(driver1.GetRunResult().Diagnostics).HasSingleItem(d => d.Id == "TUNITGEN001");
        await Assert.That(firstDiagnostic.Location.SourceTree).IsSameReferenceAs(brokenTree);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);

        var diagnostic = await Assert.That(driver2.GetRunResult().Diagnostics).HasSingleItem(d => d.Id == "TUNITGEN001");
        await Assert.That(diagnostic.Location.IsInSource).IsTrue();
        await Assert.That(diagnostic.Location.SourceTree).IsSameReferenceAs(brokenTree);
    }

    private static async Task AssertRunReasons(
        GeneratorDriver driver,
        IncrementalGeneratorRunReasons reasons,
        int outputIndex = 0
    )
    {
        var runResult = driver.GetRunResult().Results[0];

        await TestHelper.AssertRunReason(runResult, MethodAssertionGenerator.BuildAssertion, reasons.BuildStep, outputIndex);
    }
}