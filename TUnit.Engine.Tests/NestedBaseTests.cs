using FluentAssertions;

namespace TUnit.Engine.Tests;

public class NestedBaseTests : TestModule
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/NestedBaseTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Passed"),
                result => result.ResultSummary.Counters.Total.Should().Be(1),
                result => result.ResultSummary.Counters.Passed.Should().Be(1),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}