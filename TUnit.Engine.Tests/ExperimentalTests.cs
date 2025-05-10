using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class ExperimentalTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/ExperimentalTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}