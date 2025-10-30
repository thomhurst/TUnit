using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class DynamicSkipReasonTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task TestSkippedViaSetSkippedMethod_ShouldContainDynamicDeviceName()
    {
        await RunTestsWithFilter(
            "/*/*/DynamicSkipReasonTests/TestSkippedViaSetSkippedMethod",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task TestSkippedViaGetSkipReasonOverride_ShouldContainDynamicDeviceName()
    {
        await RunTestsWithFilter(
            "/*/*/DynamicSkipReasonTests/TestSkippedViaGetSkipReasonOverride",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task TestNotSkippedWhenConditionFalse_ShouldPass()
    {
        await RunTestsWithFilter(
            "/*/*/DynamicSkipReasonTests/TestNotSkippedWhenConditionFalse",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}
