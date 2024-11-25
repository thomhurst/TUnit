using FluentAssertions;

namespace TUnit.Engine.Tests;

public class UniqueBuilderContextsOnEnumerableDataGeneratorTests : TestModule
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/UniqueBuilderContextsOnEnumerableDataGeneratorTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Completed"),
                result => result.ResultSummary.Counters.Total.Should().Be(2),
                result => result.ResultSummary.Counters.Passed.Should().Be(2),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}