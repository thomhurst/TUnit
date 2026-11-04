using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

// One matrix executes in both modes. Fixtures assert inferred types and row values; counts also detect
// an unscoped or duplicate source even when its values happen to be convertible. Class cases cover
// constructor rows, type-free rows, method-derived class types and class/method source pairing.
public class GenericMethodInferencePolicyTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("_7214/ArgumentsAndTypedArgumentsRowWithGenerateGenericTest/*", 2)]
    [Arguments("_7214/TwoTypedArgumentsRowsWithGenerateGenericTest/*", 2)]
    [Arguments("_7214/TwoTypedArgumentsRows/*", 2)]
    [Arguments("_7092/GenerateGenericTestTypedDataSourceDuplicatingArgumentsTests/*", 1)]
    [Arguments("_7092/GenerateGenericTestTypedDataSourceTests/*", 2)]
    [Arguments("_7092/GenerateGenericTestCoveredByTypedDataSourceTests/*", 2)]
    [Arguments("_7092/GenerateGenericTestMatrixInferenceTests/*", 4)]
    [Arguments("_7092/GenerateGenericTestMethodDataSourceInferenceTests/*", 2)]
    [Arguments("_7092/GenerateGenericTestMultipleMethodDataSourceInferenceTests/*", 4)]
    [Arguments("_7092/GenerateGenericTestDuplicateMethodDataSourceInferenceTests/*", 3)]
    [Arguments("_7116/DeclaredTypeInferenceTests/GenerateGenericTestWithObjectRows", 1)]
    [Arguments("_7116/DeclaredTypeInferenceTests/GenerateGenericTestWithExactAndObjectRows", 2)]
    [Arguments("_7116/DeclaredTypeInferenceTests/GenerateGenericTestWithObjectAndExactRows", 2)]
    [Arguments("_7106/GenericClassSameTypeRows*/*", 3)]
    [Arguments("_7106/GenericClassMixedTypeRows*/*", 4)]
    [Arguments("_7106/GenericClassRowsTimesGenericMethodRows*/*", 4)]
    [Arguments("_7118/MixedRowsSeveralRowsPerType*/*", 5)]
    [Arguments("_7118/MixedRowsWithUninferredMethodRow*/*", 4)]
    [Arguments("_7118/MethodRowMatchedByTypedClassSource*/*", 2)]
    [Arguments("_7118/UninferredMethodRowWithTypedClassSource*/*", 4)]
    [Arguments("_7118/NullTypedClassRow*/*", 1)]
    [Arguments("_7161/ClassMethodDataSourceWithMethodRows*/*", 2)]
    [Arguments("_7161/GenerateGenericTestTypeOnlyMethodRowsInfer*/*", 2)]
    [Arguments("_7209/ClassRowInfersNoTypeArgument*/*", 2)]
    [Arguments("_7209/EmptyClassRow*/*", 2)]
    [Arguments("_7209/TypeFreeClassRowWithUninferredMethodSource*/*", 3)]
    [Arguments("_7117/MethodGenerateGenericTestWithClassRows*/*", 2)]
    [Arguments("_7117/ClassTypedMethodDataSourceOnClassGenerateGenericTest*/*", 1)]
    [Arguments("_7164/TypedSourceOnClassRow*/*", 1)]
    [Arguments("_7164/ClassRowTypedSourceAndMethodGenerateGenericTest*/*", 2)]
    [Arguments("_7164/DuplicateTypedSourcesOnGenericClass*/*", 1)]
    [Arguments("_7164/DuplicateArgumentsAndTypedSourceOnClassRow*/*", 1)]
    public async Task Sources_Respect_Precedence_And_Scope(string fixture, int expectedTests)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs.{fixture}",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
            ]);
    }

    [Test]
    public async Task Each_Class_And_Method_Row_Pair_Runs_Exactly_Once()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7451/ClassAndMethodParameterInference*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                // Compare the full collection, without Distinct: a duplicate replacing a missing
                // pair must fail even when all four tests pass. Output belongs to each test, so
                // concurrent execution cannot race on shared fixture state.
                result => result.Results
                    .SelectMany(x => (x.Output?.StdOut ?? "").Split('\n'))
                    .Select(line => line.Trim())
                    .Where(line => line.StartsWith("Issue7451 class/method pair:", StringComparison.Ordinal))
                    .OrderBy(line => line, StringComparer.Ordinal)
                    .ShouldBe([
                        "Issue7451 class/method pair: first,3",
                        "Issue7451 class/method pair: first,4",
                        "Issue7451 class/method pair: second,3",
                        "Issue7451 class/method pair: second,4"
                    ])
            ]);
    }

    [Test]
    [Arguments("_7118/MethodRowWithoutMatchingClassRow*/*", 2, "none of the class-level data rows infer")]
    [Arguments("_7161/GenericArgumentsClassRowWithUnmatchedMethodRow*/*", 2, "none of the class-level data rows infer")]
    [Arguments("_7209/ConstrainedClassWithTypeFreeClassRow*/*", 2, "struct")]
    public async Task Unmatched_And_Invalid_Class_Types_Fail_Their_Own_Row(string fixture, int expectedTests, string error)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs.{fixture}",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedTests),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedTests - 1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result =>
                {
                    var failed = result.Results.Single(x => x.Outcome == "Failed");
                    failed.Output.ShouldNotBeNull();
                    failed.Output.ErrorInfo.ShouldNotBeNull();
                    failed.Output.ErrorInfo.Message.ShouldNotBeNull();
                    failed.Output.ErrorInfo.Message.ShouldContain(error);
                },
            ]);
    }

}
