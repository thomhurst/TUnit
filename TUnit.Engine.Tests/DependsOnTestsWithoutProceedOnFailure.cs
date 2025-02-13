using Shouldly;

namespace TUnit.Engine.Tests;

public class DependsOnTestsWithoutProceedOnFailure : InvokableTestBase
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DependsOnTestsWithoutProceedOnFailure/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}