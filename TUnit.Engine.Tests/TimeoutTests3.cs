using Shouldly;

namespace TUnit.Engine.Tests;

public class TimeoutTests3 : InvokableTestBase
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/TimeoutCancellationTokenTests/DataTest",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                result => TimeSpan.Parse(result.Results[0].Duration).ShouldBeLessThan(TimeSpan.FromMinutes(1)),
                result => TimeSpan.Parse(result.Results[0].Duration).ShouldBeGreaterThan(TimeSpan.FromSeconds(4)),
            ]);
    }
}