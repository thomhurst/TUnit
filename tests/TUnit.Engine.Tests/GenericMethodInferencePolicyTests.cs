using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

// One matrix executes in both modes. Fixtures assert inferred types and row values; counts also detect
// an unscoped or duplicate source even when its values happen to be convertible.
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
}
