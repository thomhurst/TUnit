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

    [Fact]
    public void PartialContainerSplitAcrossFilesGeneratesAllMethodsOnce()
    {
        var compilation1 = Fixture.CreateLibrary(
            SplitAssertionTree(SplitAssertion),
            PartialContainerTree("IsFoo", "FooExtensions.Part1.cs"),
            PartialContainerTree("IsBar", "FooExtensions.Part2.cs"),
            HelperPartTree("1"));
        var driver1 = TestHelper.GenerateTracked<ShouldExtensionGenerator>(compilation1);

        var source = GetSource(driver1, "ShouldFooExtensions.g.cs");
        Xunit.Assert.Single(System.Text.RegularExpressions.Regex.Matches(source, @"> BeFoo\("));
        Xunit.Assert.Single(System.Text.RegularExpressions.Regex.Matches(source, @"> BeBar\("));

        // Editing the part without extension methods changes nothing the generator reads.
        var compilation2 = compilation1.ReplaceSyntaxTree(compilation1.SyntaxTrees.Last(), HelperPartTree("2"));
        var driver2 = driver1.RunGenerators(compilation2);

        // No [ShouldGeneratePartial] here, so the wrappers step never runs and is not asserted.
        var runResult = driver2.GetRunResult().Results[0];
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalContainersStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.LocalDeclarationsStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertAllRunReasons(runResult, ShouldExtensionGenerator.PayloadStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertSourceOutputsCached(runResult);
    }

    [Fact]
    public void GenericAndNestedWrappersResolveByMetadataName()
    {
        // The syntax step hands the resolver hand-built metadata names; a wrong name makes
        // GetTypeByMetadataName return null and the wrapper silently disappears. Cover a
        // dotted namespace, an arity suffix and a containing type ('+') in one go.
        var compilation = Fixture.CreateLibrary(
            SplitAssertionTree(SplitAssertion),
            SplitDeclarationsTree(),
            CSharpSyntaxTree.ParseText(
                """
                using TUnit.Assertions.Should.Attributes;

                namespace MyTests.Deeper.Wrappers
                {
                    [ShouldGeneratePartial(typeof(MyTests.FooAssertion))]
                    public partial class GenericWrapper<T>
                    {
                    }

                    public partial class Outer<TOuter>
                    {
                        [ShouldGeneratePartial(typeof(MyTests.FooAssertion))]
                        public partial class NestedWrapper
                        {
                        }
                    }
                }
                """,
                path: "Wrappers.cs"));

        var driver = TestHelper.GenerateTracked<ShouldExtensionGenerator>(compilation);

        Xunit.Assert.Contains("BeFooToo", GetSource(driver, "GenericWrapper.Generated.g.cs"));
        var nestedSource = GetSource(driver, "NestedWrapper.Generated.g.cs");
        Xunit.Assert.Contains("BeFooToo", nestedSource);
        Xunit.Assert.Contains("partial class Outer<TOuter>", nestedSource);
    }

    private static SyntaxTree HelperPartTree(string value) =>
        CSharpSyntaxTree.ParseText(
            $$"""
            namespace MyTests
            {
                public static partial class FooExtensions
                {
                    public static int Helper() => {{value}};
                }
            }
            """,
            path: "FooExtensions.Part3.cs");

    private static SyntaxTree PartialContainerTree(string methodName, string path) =>
        CSharpSyntaxTree.ParseText(
            $$"""
            using System.Runtime.CompilerServices;
            using TUnit.Assertions.Core;

            namespace MyTests
            {
                public static partial class FooExtensions
                {
                    public static FooAssertion {{methodName}}(this IAssertionSource<int> source, int expected, [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null)
                        => new FooAssertion(source.Context, expected);
                }
            }
            """,
            path: path);

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
