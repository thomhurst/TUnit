using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class DependsOnTests3(TestMode testMode) : InvokableTestBase(testMode)
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DependsOnTests3/*",
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
                    var test3Start = DateTime.Parse(result.Results.First(x => x.TestName!.StartsWith("Test3")).StartTime!);

                    test3Start.ShouldBeGreaterThanOrEqualTo(test1Start.AddSeconds(0.9));
                    test3Start.ShouldBeGreaterThanOrEqualTo(test2Start.AddSeconds(0.9));

                } 
            ]);
    }
}