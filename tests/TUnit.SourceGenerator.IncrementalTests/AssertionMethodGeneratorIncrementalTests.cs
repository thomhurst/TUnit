using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.SourceGenerator.IncrementalTests;

public class AssertionMethodGeneratorIncrementalTests
{
    private const string DefaultSource =
        """
        using TUnit.Assertions.Attributes;

        namespace MyTests;

        [AssertionFrom<char>(nameof(char.IsLetter), ExpectationMessage = "be a letter")]
        public static partial class MyCharAssertionExtensions
        {
        }

        [AssertionFrom(typeof(string), nameof(string.IsNullOrEmpty), ExpectationMessage = "be null or empty")]
        public static partial class MyStringAssertionExtensions
        {
        }

        public class Unrelated
        {
            public void Method() { }
        }
        """;

    [Test]
    public async Task GenericAndNonGenericAttributesAreDiscovered()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));

        var driver = TestHelper.GenerateTracked<AssertionMethodGenerator>(compilation);

        var hintNames = driver.GetRunResult().Results[0].GeneratedSources.Select(x => x.HintName).ToArray();
        await Assert.That(hintNames).Contains("MyCharAssertionExtensions.g.cs");
        await Assert.That(hintNames).Contains("MyStringAssertionExtensions.g.cs");
    }

    [Test]
    public async Task AddUnrelatedTypeShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<AssertionMethodGenerator>(compilation1);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);

        await AssertUnchanged(driver2.GetRunResult().Results[0]);
    }

    [Test]
    public async Task EditUnrelatedTypeShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<AssertionMethodGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "Method", "public void Method() { var x = 1; }");
        var driver2 = driver1.RunGenerators(compilation2);

        await AssertUnchanged(driver2.GetRunResult().Results[0]);
    }

    [Test]
    public async Task ModifyGenericAttributeShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<AssertionMethodGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceTypeDeclaration(compilation1, "MyCharAssertionExtensions",
            """
            [AssertionFrom<char>(nameof(char.IsLetter), ExpectationMessage = "be a letter!")]
            public static partial class MyCharAssertionExtensions
            {
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        await TestHelper.AssertAnyRunReason(runResult, AssertionMethodGenerator.GenericAssertionFromStep, IncrementalStepRunReason.Modified);
        await TestHelper.AssertAllRunReasons(runResult, AssertionMethodGenerator.NonGenericAssertionFromStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        await Assert.That(runResult.GeneratedSources.Single(x => x.HintName == "MyCharAssertionExtensions.g.cs").SourceText.ToString()).Contains("be a letter!");
    }

    [Test]
    public async Task ModifyNonGenericAttributeShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<AssertionMethodGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceTypeDeclaration(compilation1, "MyStringAssertionExtensions",
            """
            [AssertionFrom(typeof(string), nameof(string.IsNullOrWhiteSpace), ExpectationMessage = "be null or whitespace")]
            public static partial class MyStringAssertionExtensions
            {
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        await TestHelper.AssertAnyRunReason(runResult, AssertionMethodGenerator.NonGenericAssertionFromStep, IncrementalStepRunReason.Modified);
        await TestHelper.AssertAllRunReasons(runResult, AssertionMethodGenerator.GenericAssertionFromStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        await Assert.That(runResult.GeneratedSources.Single(x => x.HintName == "MyStringAssertionExtensions.g.cs").SourceText.ToString()).Contains("IsNullOrWhiteSpace");
    }

    private static async Task AssertUnchanged(GeneratorRunResult runResult)
    {
        await TestHelper.AssertAllRunReasons(runResult, AssertionMethodGenerator.GenericAssertionFromStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        await TestHelper.AssertAllRunReasons(runResult, AssertionMethodGenerator.NonGenericAssertionFromStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        await TestHelper.AssertSourceOutputsCached(runResult);
    }
}