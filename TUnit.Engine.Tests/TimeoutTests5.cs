using Shouldly;

namespace TUnit.Engine.Tests;

public class TimeoutTests5(TestMode testMode) : InvokableTestBase(testMode)
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/TimeoutCancellationTokenTests/MatrixTest",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(3),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                result => TimeSpan.Parse(result.Results[0].Duration).ShouldBeLessThan(TimeSpan.FromMinutes(1)),
                result => TimeSpan.Parse(result.Results[0].Duration).ShouldBeGreaterThan(TimeSpan.FromSeconds(4)),
            ]);
    }
}