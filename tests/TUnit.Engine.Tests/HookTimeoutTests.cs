using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class HookTimeoutTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task ClassHook_WithTimeout_ShouldFail()
    {
        await RunTestsWithFilter(
            "/*/*/ClassHookTimeoutTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
            ]);
    }

    [Test]
    public async Task AssemblyHook_WithTimeout_ShouldPass()
    {
        await RunTestsWithFilter(
            "/*/*/AssemblyHookTimeoutPassTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
            ]);
    }
}
