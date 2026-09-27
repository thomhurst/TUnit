using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class ClearParallelizationTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task ClearedConstraintsAndLimiter_LetTestsRunTogether()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._6892/ClearParallelizationTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task LimiterSetAfterClear_IsApplied()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._6892/ClearParallelLimiterThenSetTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}
