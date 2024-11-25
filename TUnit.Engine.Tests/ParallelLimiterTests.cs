using FluentAssertions;

namespace TUnit.Engine.Tests;

public class ParallelLimiterTests : TestModule
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/ParallelLimiterTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Completed"),
                result => result.ResultSummary.Counters.Total.Should().Be(12),
                result => result.ResultSummary.Counters.Passed.Should().Be(12),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}