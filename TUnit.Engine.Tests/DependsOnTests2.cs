using FluentAssertions;

namespace TUnit.Engine.Tests;

public class DependsOnTests2 : TestModule
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DependsOnTests2/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Completed"),
                result => result.ResultSummary.Counters.Total.Should().Be(3),
                result => result.ResultSummary.Counters.Passed.Should().Be(3),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0),
                result =>
                {
                    var test1Start = DateTime.Parse(result.Results.First(x => x.TestName!.StartsWith("Test1")).StartTime!);
                    var test2Start = DateTime.Parse(result.Results.First(x => x.TestName!.StartsWith("Test2")).StartTime!);

                    test2Start.Should().BeOnOrAfter(test1Start.AddSeconds(4.9));
                }
            ]);
    }
}