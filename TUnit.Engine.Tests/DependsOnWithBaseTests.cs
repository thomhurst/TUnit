using FluentAssertions;

namespace TUnit.Engine.Tests;

public class DependsOnWithBaseTests : InvokableTestBase
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DependsOnWithBaseTests/*",
            [
                result => result.ResultSummary.Outcome.Should().Be("Completed"),
                result => result.ResultSummary.Counters.Total.Should().Be(2),
                result => result.ResultSummary.Counters.Passed.Should().Be(2),
                result => result.ResultSummary.Counters.Failed.Should().Be(0),
                result => result.ResultSummary.Counters.NotExecuted.Should().Be(0),
                result =>
                {
                    // var baseTest = result.TrxReport.UnitTestResults.First(x => x.TestName!.StartsWith("SubTypeTest")).StartTime!;
                    // var subTypeTestStart = result.TrxReport.UnitTestResults.First(x => x.TestName!.StartsWith("BaseTest")).StartTime!;
                    //
                    // subTypeTestStart.Should().BeOnOrAfter(baseTest.AddSeconds(4.9));
                }
            ]);
    }
}