using Shouldly;
using TUnit.Core.Enums;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class TestExecutionCancellationTests(TestMode testMode) : InvokableTestBase(testMode)
{
    private const string StateRecordingRunId = "TUNIT_CANCELLATION_STATE_RUN_ID";
    private const string RetryBackoffCancellationClassName = "RetryBackoffCancellationTests";
    private const string CancelDuringRetryTransitionClassName = "CancelDuringRetryTransitionTests";
    private const string CancelAndThrowDuringRetryClassName = "CancelAndThrowDuringRetryTests";

    [Test]
    public async Task CancelMarksCurrentTestExecutionAsCancelled()
    {
        var runId = Guid.NewGuid().ToString("N");
        var stateDirectory = Path.Combine(Path.GetTempPath(), nameof(TestExecutionCancellationTests), runId);

        try
        {
            await RunTestsWithFilter(
                "/*/*/TestExecutionCancellationTests/*",
                [
                    result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                    result => result.ResultSummary.Counters.Total.ShouldBe(2),
                    result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                    result => result.ResultSummary.Counters.Failed.ShouldBe(2),
                    _ => ReadState("CancelStopsCooperativeTestBody").ShouldBe(TestState.Cancelled.ToString()),
                    _ => ReadState("CancelMarksTestAsCancelledWhenBodyReturnsNormally").ShouldBe(TestState.Cancelled.ToString())
                ],
                new RunOptions().WithEnvironmentVariable(StateRecordingRunId, runId));
        }
        finally
        {
            if (Directory.Exists(stateDirectory))
            {
                Directory.Delete(stateDirectory, recursive: true);
            }
        }

        string ReadState(string testName) => File.ReadAllText(Path.Combine(stateDirectory, $"{testName}.txt"));
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

    [Test]
    public async Task CancelDuringRetryBackoffStopsTheRetry()
    {
        var runId = Guid.NewGuid().ToString("N");
        var stateDirectory = Path.Combine(Path.GetTempPath(), RetryBackoffCancellationClassName, runId);

        try
        {
            await RunTestsWithFilter(
                "/*/*/RetryBackoffCancellationTests/*",
                [
                    result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                    result => result.ResultSummary.Counters.Total.ShouldBe(1),
                    result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                    result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                    _ => File.ReadAllText(Path.Combine(stateDirectory, "FinalState.txt"))
                        .ShouldBe(TestState.Cancelled.ToString())
                ],
                new RunOptions().WithEnvironmentVariable(StateRecordingRunId, runId));
        }
        finally
        {
            if (Directory.Exists(stateDirectory))
            {
                Directory.Delete(stateDirectory, recursive: true);
            }
        }
    }

    [Test]
    [Arguments(CancelDuringRetryTransitionClassName)]
    [Arguments(CancelAndThrowDuringRetryClassName)]
    public async Task CancelDuringRetryTransitionStopsTheRetry(string className)
    {
        var runId = Guid.NewGuid().ToString("N");
        var stateDirectory = Path.Combine(Path.GetTempPath(), className, runId);

        try
        {
            await RunTestsWithFilter(
                $"/*/*/{className}/*",
                [
                    result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                    result => result.ResultSummary.Counters.Total.ShouldBe(1),
                    result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                    result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                    _ => File.ReadAllText(Path.Combine(stateDirectory, "FinalState.txt"))
                        .ShouldBe(TestState.Cancelled.ToString())
                ],
                new RunOptions().WithEnvironmentVariable(StateRecordingRunId, runId));
        }
        finally
        {
            if (Directory.Exists(stateDirectory))
            {
                Directory.Delete(stateDirectory, recursive: true);
            }
        }
    }
}
