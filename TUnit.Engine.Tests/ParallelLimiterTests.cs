using Shouldly;

namespace TUnit.Engine.Tests;

public class ParallelLimiterTests : InvokableTestBase
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/ParallelLimiterTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(12),
                result => result.ResultSummary.Counters.Passed.ShouldBe(12),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}