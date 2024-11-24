using FluentAssertions;

namespace TUnit.Engine.Tests;

public class UniqueObjectsOnEnumerableDataGeneratorTests : TestModule
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/UniqueObjectsOnEnumerableDataGeneratorTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Passed"),
                result => result.ResultSummary.Counters.Total.Should().Be(9),
                result => result.ResultSummary.Counters.Passed.Should().Be(9),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}