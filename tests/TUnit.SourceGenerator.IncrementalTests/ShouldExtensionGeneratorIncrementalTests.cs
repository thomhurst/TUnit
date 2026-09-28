extern alias ShouldGenerator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ShouldGenerator::TUnit.Assertions.Should.SourceGenerator;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

public class ShouldExtensionGeneratorIncrementalTests
{
    private const string DefaultSource =
        """
        using System;
        using System.Runtime.CompilerServices;
        using System.Threading.Tasks;
        using TUnit.Assertions.Core;
        using TUnit.Assertions.Should.Attributes;

        namespace TUnit.Assertions.Should.Attributes
        {
            [AttributeUsage(AttributeTargets.Class)]
            public sealed class ShouldGeneratePartialAttribute : Attribute
            {
                public ShouldGeneratePartialAttribute(Type wrappedType) { }
            }
        }

        namespace MyTests
        {
            public class FooAssertion : Assertion<int>
            {
                public FooAssertion(AssertionContext<int> context, int expected) : base(context) { }

                public FooAssertion IsFooToo(int expected) => new FooAssertion(Context, expected);

                protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
                    => Task.FromResult(AssertionResult.Passed);

                protected override string GetExpectation() => "to be foo";
            }

            public static class FooExtensions
            {
                public static FooAssertion IsFoo(this IAssertionSource<int> source, int expected, [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null)
                    => new FooAssertion(source.Context, expected);
            }

            [ShouldGeneratePartial(typeof(FooAssertion))]
            public partial class FooWrapper
            {
            }

            public class Unrelated
            {
                public void Method() { }
            }
        }
        """;

    [Fact]
    public void GeneratesFromLocalContainersAndWrappers()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));

        var driver = TestHelper.GenerateTracked<ShouldExtensionGenerator>(compilation);

        var sources = driver.GetRunResult().Results[0].GeneratedSources;
        Xunit.Assert.Contains("BeFoo", sources.Single(x => x.HintName == "ShouldFooExtensions.g.cs").SourceText.ToString());
        Xunit.Assert.Contains("BeFooToo", sources.Single(x => x.HintName == "FooWrapper.Generated.g.cs").SourceText.ToString());
    }

    [Fact]
    public void AddUnrelatedTypeShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<ShouldExtensionGenerator>(compilation1);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);

        AssertUnchanged(driver2.GetRunResult().Results[0]);
    }

    [Fact]
    public void EditUnrelatedTypeShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<ShouldExtensionGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "Method", "public void Method() { var x = 1; }");
        var driver2 = driver1.RunGenerators(compilation2);

        AssertUnchanged(driver2.GetRunResult().Results[0]);
    }

    [Fact]
    public void ModifyExtensionMethodShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<ShouldExtensionGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "IsFoo",
            """
            public static FooAssertion IsFooish(this IAssertionSource<int> source, int expected, [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null)
                => new FooAssertion(source.Context, expected);
            """);
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalWrappersStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalDeclarationsStep, IncrementalStepRunReason.Modified);
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.PayloadStep, IncrementalStepRunReason.Modified);
        Xunit.Assert.Contains("BeFooish", runResult.GeneratedSources.Single(x => x.HintName == "ShouldFooExtensions.g.cs").SourceText.ToString());
    }

    [Fact]
    public void ModifyWrapperShouldRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<ShouldExtensionGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceTypeDeclaration(compilation1, "FooWrapper",
            """
            [ShouldGeneratePartial(typeof(FooAssertion))]
            internal partial class FooWrapper
            {
                public void BeFooToo(int expected) { }
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalContainersStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.PayloadStep, IncrementalStepRunReason.Modified);
        Xunit.Assert.DoesNotContain(runResult.GeneratedSources, x => x.HintName == "FooWrapper.Generated.g.cs");
    }

    [Fact]
    public void ModifyWrappedAssertionInOtherFileShouldRegenerateWrapper()
    {
        var compilation1 = Fixture.CreateLibrary(SplitAssertionTree(SplitAssertion), SplitDeclarationsTree());
        var driver1 = TestHelper.GenerateTracked<ShouldExtensionGenerator>(compilation1);
        Xunit.Assert.Contains("BeFooToo", GetSource(driver1, "FooWrapper.Generated.g.cs"));

        var compilation2 = compilation1.ReplaceSyntaxTree(
            compilation1.SyntaxTrees.First(),
            SplitAssertionTree(SplitAssertion.Replace("IsFooToo", "IsFooThree")));
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalWrappersStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalDeclarationsStep, IncrementalStepRunReason.Modified);
        var wrapperSource = GetSource(driver2, "FooWrapper.Generated.g.cs");
        Xunit.Assert.Contains("BeFooThree", wrapperSource);
        Xunit.Assert.DoesNotContain("BeFooToo", wrapperSource);
    }

    [Fact]
    public void ModifyReturnedAssertionInOtherFileShouldRegenerateContainer()
    {
        var compilation1 = Fixture.CreateLibrary(SplitAssertionTree(SplitAssertion), SplitDeclarationsTree());
        var driver1 = TestHelper.GenerateTracked<ShouldExtensionGenerator>(compilation1);
        Xunit.Assert.Contains("BeFoo", GetSource(driver1, "ShouldFooExtensions.g.cs"));

        // The extension no longer maps onto a constructor, so its Should counterpart must disappear.
        var compilation2 = compilation1.ReplaceSyntaxTree(
            compilation1.SyntaxTrees.First(),
            SplitAssertionTree(SplitAssertion.Replace(
                "public FooAssertion(AssertionContext<int> context, int expected)",
                "public FooAssertion(AssertionContext<int> context, string expected)")));
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalContainersStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalDeclarationsStep, IncrementalStepRunReason.Modified);
        Xunit.Assert.DoesNotContain(runResult.GeneratedSources, x => x.HintName == "ShouldFooExtensions.g.cs");
    }

    private const string SplitAssertion =
        """
        using System.Threading.Tasks;
        using TUnit.Assertions.Core;

        namespace MyTests
        {
            public class FooAssertion : Assertion<int>
            {
                public FooAssertion(AssertionContext<int> context, int expected) : base(context) { }

                public FooAssertion IsFooToo(int expected) => new FooAssertion(Context, expected);

                protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
                    => Task.FromResult(AssertionResult.Passed);

                protected override string GetExpectation() => "to be foo";
            }
        }
        """;

    private static SyntaxTree SplitAssertionTree(string source) =>
        CSharpSyntaxTree.ParseText(source, path: "FooAssertion.cs");

    private static SyntaxTree SplitDeclarationsTree() =>
        CSharpSyntaxTree.ParseText(
            """
            using System;
            using System.Runtime.CompilerServices;
            using TUnit.Assertions.Core;
            using TUnit.Assertions.Should.Attributes;

            namespace TUnit.Assertions.Should.Attributes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public sealed class ShouldGeneratePartialAttribute : Attribute
                {
                    public ShouldGeneratePartialAttribute(Type wrappedType) { }
                }
            }

            namespace MyTests
            {
                public static class FooExtensions
                {
                    public static FooAssertion IsFoo(this IAssertionSource<int> source, int expected, [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null)
                        => new FooAssertion(source.Context, expected);
                }

                [ShouldGeneratePartial(typeof(FooAssertion))]
                public partial class FooWrapper
                {
                }
            }
            """,
            path: "Declarations.cs");

    private static string GetSource(GeneratorDriver driver, string hintName) =>
        driver.GetRunResult().Results[0].GeneratedSources.Single(x => x.HintName == hintName).SourceText.ToString();

    private static void AssertUnchanged(GeneratorRunResult runResult)
    {
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalContainersStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalWrappersStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalDeclarationsStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.CompilationDataStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.PayloadStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertSourceOutputsCached(runResult);
    }
}
