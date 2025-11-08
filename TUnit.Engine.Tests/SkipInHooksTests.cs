using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class SkipInHooksTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task SkipInBeforeClassHook_ShouldMarkTestAsSkipped()
    {
        await RunTestsWithFilter(
            "/*/*/SkipInBeforeClassHookTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task SkipInBeforeTestHook_ShouldMarkAllTestsAsSkipped()
    {
        await RunTestsWithFilter(
            "/*/*/SkipInBeforeTestHookTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(2)
            ]);
    }
}
