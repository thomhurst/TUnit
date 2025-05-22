using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class DependencyCountTests(TestMode testMode) : InvokableTestBase(testMode)
{
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/DependencyCountTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(8),
                result => result.ResultSummary.Counters.Passed.ShouldBe(8),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
            ]);
    }
}