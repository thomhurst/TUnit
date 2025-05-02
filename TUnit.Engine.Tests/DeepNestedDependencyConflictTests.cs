using Shouldly;

namespace TUnit.Engine.Tests;

public class DeepNestedDependencyConflictTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DeepNestedDependencyConflict/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(10),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(10),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}