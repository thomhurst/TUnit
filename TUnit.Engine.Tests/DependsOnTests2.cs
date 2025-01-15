using Shouldly;

namespace TUnit.Engine.Tests;

public class DependsOnTests2 : InvokableTestBase
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DependsOnTests2/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                result =>
                {
                    var test1Start = DateTime.Parse(result.Results.First(x => x.TestName!.StartsWith("Test1")).StartTime!);
                    var test2Start = DateTime.Parse(result.Results.First(x => x.TestName!.StartsWith("Test2")).StartTime!);

                    test2Start.ShouldBeGreaterThanOrEqualTo(test1Start.AddSeconds(4.9));
                }
            ]);
    }
}