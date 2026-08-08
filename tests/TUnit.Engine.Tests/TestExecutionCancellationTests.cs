using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class TestExecutionCancellationTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task CancelMarksCurrentTestExecutionAsCancelled()
    {
        await RunTestsWithFilter(
            "/*/*/TestExecutionCancellationTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(2)
            ]);
    }

    [Test]
    public async Task CancelAfterTheTestExecutionCompletesIsIgnored()
    {
        await RunTestsWithFilter(
            "/*/*/LateTestExecutionCancellationTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
