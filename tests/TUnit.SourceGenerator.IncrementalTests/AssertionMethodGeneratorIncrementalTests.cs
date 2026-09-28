using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

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

    [Fact]
    public void GenericAndNonGenericAttributesAreDiscovered()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));

        var driver = TestHelper.GenerateTracked<AssertionMethodGenerator>(compilation);

        var hintNames = driver.GetRunResult().Results[0].GeneratedSources.Select(x => x.HintName).ToArray();
        Xunit.Assert.Contains("MyCharAssertionExtensions.g.cs", hintNames);
        Xunit.Assert.Contains("MyStringAssertionExtensions.g.cs", hintNames);
    }

    [Fact]
    public void AddUnrelatedTypeShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<AssertionMethodGenerator>(compilation1);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);

        AssertUnchanged(driver2.GetRunResult().Results[0]);
    }

    [Fact]
    public void EditUnrelatedTypeShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<AssertionMethodGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "Method", "public void Method() { var x = 1; }");
        var driver2 = driver1.RunGenerators(compilation2);

        AssertUnchanged(driver2.GetRunResult().Results[0]);
    }

    [Fact]
    public void ModifyGenericAttributeShouldRegenerate()
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
        TestHelper.AssertAnyRunReason(runResult, AssertionMethodGenerator.GenericAssertionFromStep, IncrementalStepRunReason.Modified);
        TestHelper.AssertAllRunReasons(runResult, AssertionMethodGenerator.NonGenericAssertionFromStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        Xunit.Assert.Contains("be a letter!", runResult.GeneratedSources.Single(x => x.HintName == "MyCharAssertionExtensions.g.cs").SourceText.ToString());
    }

    [Fact]
    public void ModifyNonGenericAttributeShouldRegenerate()
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
        TestHelper.AssertAnyRunReason(runResult, AssertionMethodGenerator.NonGenericAssertionFromStep, IncrementalStepRunReason.Modified);
        TestHelper.AssertAllRunReasons(runResult, AssertionMethodGenerator.GenericAssertionFromStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        Xunit.Assert.Contains("IsNullOrWhiteSpace", runResult.GeneratedSources.Single(x => x.HintName == "MyStringAssertionExtensions.g.cs").SourceText.ToString());
    }

    private static void AssertUnchanged(GeneratorRunResult runResult)
    {
        TestHelper.AssertAllRunReasons(runResult, AssertionMethodGenerator.GenericAssertionFromStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertAllRunReasons(runResult, AssertionMethodGenerator.NonGenericAssertionFromStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertSourceOutputsCached(runResult);
    }
}
