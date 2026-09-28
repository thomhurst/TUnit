using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

public class AssertionExtensionGeneratorIncrementalTests
{
    private const string DefaultSource =
        """
        using System.Threading.Tasks;
        using TUnit.Assertions.Attributes;
        using TUnit.Assertions.Core;

        namespace MyTests;

        [AssertionExtension("IsFoo")]
        public class FooAssertion : Assertion<int>
        {
            private readonly int _expected;

            public FooAssertion(AssertionContext<int> context, int expected) : base(context)
            {
                _expected = expected;
            }

            protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
                => Task.FromResult(metadata.Value == _expected ? AssertionResult.Passed : AssertionResult.Failed("not foo"));

            protected override string GetExpectation() => "to be foo";
        }

        [AssertionExtension("IsBar")]
        public class BarAssertion : Assertion<string>
        {
            public BarAssertion(AssertionContext<string> context, string expected = "bar") : base(context)
            {
            }

            protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
                => Task.FromResult(AssertionResult.Passed);

            protected override string GetExpectation() => "to be bar";
        }

        public class Unrelated
        {
            public void Method() { }
        }
        """;

    [Fact]
    public void AddUnrelatedTypeShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<AssertionExtensionGenerator>(compilation1);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        TestHelper.AssertAllRunReasons(runResult, AssertionExtensionGenerator.AssertionExtensionStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertSourceOutputsCached(runResult);
    }

    [Fact]
    public void EditUnrelatedTypeShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<AssertionExtensionGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "Method", "public void Method() { var x = 1; }");
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        TestHelper.AssertAllRunReasons(runResult, AssertionExtensionGenerator.AssertionExtensionStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        TestHelper.AssertSourceOutputsCached(runResult);
    }

    [Fact]
    public void ModifyAssertionClassShouldRegenerateOnlyThatClass()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<AssertionExtensionGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceTypeDeclaration(compilation1, "BarAssertion",
            """
            [AssertionExtension("IsBaz")]
            public class BarAssertion : Assertion<string>
            {
                public BarAssertion(AssertionContext<string> context, string expected = "bar") : base(context)
                {
                }

                protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<string> metadata)
                    => Task.FromResult(AssertionResult.Passed);

                protected override string GetExpectation() => "to be bar";
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        var reasons = runResult.TrackedSteps[AssertionExtensionGenerator.AssertionExtensionStep]
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason)
            .ToArray();
        Xunit.Assert.Equal(2, reasons.Length);
        Xunit.Assert.Contains(reasons[0], new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged });
        Xunit.Assert.Equal(IncrementalStepRunReason.Modified, reasons[1]);

        var sourceOutputReasons = runResult.TrackedOutputSteps
            .SelectMany(x => x.Value)
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason)
            .ToArray();
        Xunit.Assert.Single(sourceOutputReasons, IncrementalStepRunReason.Modified);
        Xunit.Assert.Contains("IsBaz", runResult.GeneratedSources.Single(x => x.HintName == "BarAssertion.Extensions.g.cs").SourceText.ToString());
    }
}
