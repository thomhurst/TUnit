using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class ConsoleTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/ConsoleTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}