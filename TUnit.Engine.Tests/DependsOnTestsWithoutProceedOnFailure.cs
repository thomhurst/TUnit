using FluentAssertions;

namespace TUnit.Engine.Tests;

public class DependsOnTestsWithoutProceedOnFailure : InvokableTestBase
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DependsOnTestsWithoutProceedOnFailure/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Failed"),
                result => result.ResultSummary.Counters.Total.Should().Be(2),
                result => result.ResultSummary.Counters.Passed.Should().Be(0),
                result => result.ResultSummary.Counters.Failed.Should().Be(2),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0)
            ]);
    }
}