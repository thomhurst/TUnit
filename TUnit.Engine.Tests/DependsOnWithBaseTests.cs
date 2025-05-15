using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class DependsOnWithBaseTests(TestMode testMode) : InvokableTestBase(testMode)
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DependsOnWithBaseTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                result =>
                {
                    // var baseTest = result.TrxReport.UnitTestResults.First(x => x.TestName!.StartsWith("SubTypeTest")).StartTime!;
                    // var subTypeTestStart = result.TrxReport.UnitTestResults.First(x => x.TestName!.StartsWith("BaseTest")).StartTime!;
                    //
                    // subTypeTestStart.ShouldBeGreaterThanOrEqualTo(baseTest.AddSeconds(4.9));
                }
            ]);
    }
}