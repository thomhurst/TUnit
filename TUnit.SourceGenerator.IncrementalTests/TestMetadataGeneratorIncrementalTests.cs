using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

/// <summary>
/// Tests that TestMetadataGenerator's incremental pipeline properly caches
/// when unrelated code changes, and re-runs when test methods change.
/// </summary>
public class TestMetadataGeneratorIncrementalTests
{
    private const string SimpleTestClass =
        """
        using global::TUnit.Core;

        public class MyTests
        {
            [Test]
            public void Test1()
            {
            }
        }
        """;

    private const string TestClassWithArguments =
        """
        using global::TUnit.Core;

        public class ArgTests
        {
            [Test]
            [Arguments("hello")]
            public void Test1(string value)
            {
            }
        }
        """;

    private const string TestClassWithArrayArguments =
        """
        using global::TUnit.Core;

        public class ArrayArgTests
        {
            [Test]
            [Arguments(new[] { 1, 2, 3 })]
            public void Test1(int[] values)
            {
            }
        }
        """;

    /// <summary>
    /// Adding an unrelated type should NOT cause test metadata to regenerate.
    /// </summary>
    [Fact]
    public void AddUnrelatedType_ShouldNotRegenerateTestMetadata()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SimpleTestClass, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<TestMetadataGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // Add an unrelated struct — should NOT invalidate test metadata
        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct UnrelatedValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    /// <summary>
    /// Changing an attribute on a test method SHOULD cause regeneration.
    /// </summary>
    [Fact]
    public void ChangeTestAttribute_ShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(TestClassWithArguments, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<TestMetadataGenerator>(compilation1);
        AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // Change the Arguments attribute value
        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "Test1",
            """
            [Test]
            [Arguments("world")]
            public void Test1(string value)
            {
            }
            """);

        var driver2 = driver1.RunGenerators(compilation2);
        AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified);
    }

    /// <summary>
    /// Tests that ComputeAttributeHash handles array-typed constructor arguments
    /// without throwing (TypedConstant.Value throws for arrays — must use Values).
    /// </summary>
    [Fact]
    public void ArrayArguments_ShouldNotThrow()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(TestClassWithArrayArguments, CSharpParseOptions.Default);
        var compilation = Fixture.CreateLibrary(syntaxTree);

        var driver = TestHelper.GenerateTracked<TestMetadataGenerator>(compilation);
        var result = driver.GetRunResult();

        // Verify no diagnostics from our generator
        var generatorDiagnostics = result.Results
            .SelectMany(r => r.Diagnostics)
            .Where(d => d.Id.StartsWith("TUNIT"))
            .ToList();
        Xunit.Assert.Empty(generatorDiagnostics);
    }

    private static void AssertRunReasons(
        GeneratorDriver driver,
        IncrementalGeneratorRunReasons reasons,
        int outputIndex = 0)
    {
        var runResult = driver.GetRunResult().Results[0];
        TestHelper.AssertRunReason(runResult, TestMetadataGenerator.ParseTestMetadata, reasons.BuildStep, outputIndex);
    }
}
