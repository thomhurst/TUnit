using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class PriorityFilteringTests1(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/PriorityFilteringTests/*[Priority=High]",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}