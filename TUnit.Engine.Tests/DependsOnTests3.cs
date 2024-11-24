using FluentAssertions;

namespace TUnit.Engine.Tests;

public class DependsOnTests3 : TestModule
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DependsOnTests3/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Passed"),
                result => result.ResultSummary.Counters.Total.Should().Be(3),
                result => result.ResultSummary.Counters.Passed.Should().Be(3),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0),
                result =>
                {
                    var test1Start = DateTime.Parse(result.Results.First(x => x.TestName!.StartsWith("Test1")).StartTime!);
                    var test2Start = DateTime.Parse(result.Results.First(x => x.TestName!.StartsWith("Test2")).StartTime!);
                    var test3Start = DateTime.Parse(result.Results.First(x => x.TestName!.StartsWith("Test3")).StartTime!);

                    test3Start.Should().BeOnOrAfter(test1Start.AddSeconds(0.9));
                    test3Start.Should().BeOnOrAfter(test2Start.AddSeconds(0.9));

                } 
            ]);
    }
}