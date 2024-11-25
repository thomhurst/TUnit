using FluentAssertions;

namespace TUnit.Engine.Tests;

public class TimeoutTests1 : InvokableTestBase
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/TimeoutCancellationTokenTests/BasicTest",
            [
                result => result.ResultSummary.Outcome.Should().Be("Failed"),
                result => result.ResultSummary.Counters.Total.Should().Be(1),
                result => result.ResultSummary.Counters.Passed.Should().Be(0),
                result => result.ResultSummary.Counters.Failed.Should().Be(1),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0),
                result => TimeSpan.Parse(result.Results[0].Duration).Should().BeLessThan(TimeSpan.FromMinutes(1)),
                result => TimeSpan.Parse(result.Results[0].Duration).Should().BeGreaterThan(TimeSpan.FromSeconds(4)),
            ]);
    }
}