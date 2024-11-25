using FluentAssertions;

namespace TUnit.Engine.Tests;

public class RetryTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/RetryTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Failed"),
                result => result.ResultSummary.Counters.Total.Should().Be(4),
                result => result.ResultSummary.Counters.Passed.Should().Be(1),
                result => result.ResultSummary.Counters.Failed.Should().Be(3),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}