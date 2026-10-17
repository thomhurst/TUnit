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

    [Test]
    public async Task AddUnrelatedTypeShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<AssertionExtensionGenerator>(compilation1);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        await TestHelper.AssertAllRunReasons(runResult, AssertionExtensionGenerator.AssertionExtensionStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        await TestHelper.AssertSourceOutputsCached(runResult);
    }

    [Test]
    public async Task EditUnrelatedTypeShouldNotRegenerate()
    {
        var compilation1 = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(DefaultSource));
        var driver1 = TestHelper.GenerateTracked<AssertionExtensionGenerator>(compilation1);

        var compilation2 = TestHelper.ReplaceMethodDeclaration(compilation1, "Method", "public void Method() { var x = 1; }");
        var driver2 = driver1.RunGenerators(compilation2);

        var runResult = driver2.GetRunResult().Results[0];
        await TestHelper.AssertAllRunReasons(runResult, AssertionExtensionGenerator.AssertionExtensionStep,
            IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged);
        await TestHelper.AssertSourceOutputsCached(runResult);
    }

    [Test]
    public async Task ModifyAssertionClassShouldRegenerateOnlyThatClass()
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
        await Assert.That(reasons.Length).IsEqualTo(2);
        await Assert.That(new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged }).Contains(reasons[0]);
        await Assert.That(reasons[1]).IsEqualTo(IncrementalStepRunReason.Modified);

        var sourceOutputReasons = runResult.TrackedOutputSteps
            .SelectMany(x => x.Value)
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason)
            .ToArray();
        await Assert.That(sourceOutputReasons).HasSingleItem(x => x == IncrementalStepRunReason.Modified);
        await Assert.That(runResult.GeneratedSources.Single(x => x.HintName == "BarAssertion.Extensions.g.cs").SourceText.ToString()).Contains("IsBaz");
    }
}