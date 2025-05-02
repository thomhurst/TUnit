using Shouldly;

namespace TUnit.Engine.Tests;

public class NotInParallelWithDependsOnTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/NotInParallelWithDependsOnTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(3),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),

            ]);
    }
}