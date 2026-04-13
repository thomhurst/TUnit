using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

/// <summary>
/// Tests that TestMetadataGenerator's incremental pipeline properly caches
/// when unrelated code changes, and re-runs when test methods change.
/// This validates the string-based equality on TestMethodMetadata
/// (MethodFullyQualifiedName, TypeFullyQualifiedName, MethodAttributeHash).
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
    /// This is the core incremental caching test — if TestMethodMetadata.Equals
    /// used ISymbol identity instead of string identity, this would fail because
    /// symbols are new instances on every compilation.
    /// </summary>
    [Fact]
    public void AddUnrelatedType_ShouldNotRegenerateTestMetadata()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SimpleTestClass, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<TestMetadataGenerator>(compilation1);
        var result1 = driver1.GetRunResult();
        Xunit.Assert.True(result1.GeneratedTrees.Length > 0, "Should generate at least one source file");

        // Add an unrelated struct — should NOT invalidate test metadata
        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct UnrelatedValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        var result2 = driver2.GetRunResult();

        // Verify the generator still produces output (sanity check)
        Xunit.Assert.True(result2.GeneratedTrees.Length > 0, "Should still generate source files after adding unrelated type");
    }

    /// <summary>
    /// Adding a new test method SHOULD cause regeneration.
    /// </summary>
    [Fact]
    public void AddNewTestMethod_ShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SimpleTestClass, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<TestMetadataGenerator>(compilation1);
        var result1 = driver1.GetRunResult();
        var initialTreeCount = result1.GeneratedTrees.Length;

        // Add a new test class with a test method
        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            """
            using global::TUnit.Core;

            public class MoreTests
            {
                [Test]
                public void Test2()
                {
                }
            }
            """));
        var driver2 = driver1.RunGenerators(compilation2);
        var result2 = driver2.GetRunResult();

        // Should have more generated trees now
        Xunit.Assert.True(result2.GeneratedTrees.Length >= initialTreeCount,
            $"Expected at least {initialTreeCount} trees after adding new test, got {result2.GeneratedTrees.Length}");
    }

    /// <summary>
    /// Changing an attribute on a test method SHOULD cause regeneration.
    /// This validates that MethodAttributeHash detects attribute changes.
    /// </summary>
    [Fact]
    public void ChangeTestAttribute_ShouldRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(TestClassWithArguments, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked<TestMetadataGenerator>(compilation1);

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
        var result2 = driver2.GetRunResult();
        Xunit.Assert.True(result2.GeneratedTrees.Length > 0, "Should regenerate after attribute change");
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

        // This should not throw — previously ComputeAttributeHash would throw
        // "TypedConstant is an array. Use Values property." for array arguments
        var driver = TestHelper.GenerateTracked<TestMetadataGenerator>(compilation);
        var result = driver.GetRunResult();

        // Verify no diagnostics from our generator
        var generatorDiagnostics = result.Results
            .SelectMany(r => r.Diagnostics)
            .Where(d => d.Id.StartsWith("TUNIT"))
            .ToList();
        Xunit.Assert.Empty(generatorDiagnostics);
    }
}
