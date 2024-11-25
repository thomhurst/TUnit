using FluentAssertions;

namespace TUnit.Engine.Tests;

public class FailTests : InvokableTestBase
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/PassFailTests/*[Category=Fail]",
            [
                result => result.ResultSummary.Outcome.Should().Be("Failed"),
                result => result.ResultSummary.Counters.Total.Should().Be(86),
                result => result.ResultSummary.Counters.Passed.Should().Be(0),
                result => result.ResultSummary.Counters.Failed.Should().Be(86),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}