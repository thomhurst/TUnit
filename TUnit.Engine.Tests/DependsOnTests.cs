using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class DependsOnTests(TestMode testMode) : InvokableTestBase(testMode)
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DependsOnTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
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