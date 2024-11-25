using FluentAssertions;

namespace TUnit.Engine.Tests;

public class NotInParallelWithDependsOnTests : TestModule
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/NotInParallelWithDependsOnTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Failed"),
                result => result.ResultSummary.Counters.Total.Should().Be(3),
                result => result.ResultSummary.Counters.Passed.Should().Be(0),
                result => result.ResultSummary.Counters.Failed.Should().Be(3),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0),

            ]);
    }
}