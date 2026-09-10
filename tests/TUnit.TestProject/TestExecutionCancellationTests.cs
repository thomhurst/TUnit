using TUnit.Core.Executors;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
[TestExecutor<CancellationObservingTestExecutor>]
public class TestExecutionCancellationTests
{
    private const string StateRecordingRunId = "TUNIT_CANCELLATION_STATE_RUN_ID";

    [Test]
    [Timeout(5_000)]
    public async Task CancelStopsCooperativeTestBody(CancellationToken cancellationToken)
    {
        var execution = TestContext.Current!.Execution;

        ThreadPool.QueueUserWorkItem(static state => ((ITestExecution)state!).Cancel(), execution);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    [Test]
    public void CancelMarksTestAsCancelledWhenBodyReturnsNormally()
    {
        TestContext.Current!.Execution.Cancel();
    }

    [After(Test)]
    public void RecordFinalState(TestContext context)
    {
        if (Environment.GetEnvironmentVariable(StateRecordingRunId) is not { Length: > 0 } runId)
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), nameof(TestExecutionCancellationTests), runId);
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, $"{context.Metadata.TestDetails.MethodName}.txt"),
            context.Execution.Result?.State.ToString() ?? "No result");
    }
}

public sealed class CancellationObservingTestExecutor : ITestExecutor
{
    public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        using var executorCancellationSource = new CancellationTokenSource();
        context.Execution.AddLinkedCancellationToken(executorCancellationSource.Token);

        try
        {
            await action();
        }
        catch (OperationCanceledException) when (context.Execution.CancellationToken.IsCancellationRequested)
        {
            // Custom executors may consume cooperative cancellation. The execution result
            // must still remain cancelled when the executor returns successfully.
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
public class LateTestExecutionCancellationTests
{
    private static ITestExecution? _completedExecution;

    [Test]
    public void CaptureExecution()
    {
        _completedExecution = TestContext.Current!.Execution;
    }

    [Test]
    [DependsOn(nameof(CaptureExecution))]
    public void CancelAfterPreviousTestIsDisposed()
    {
        _completedExecution!.Cancel();
    }
}

[EngineTest(ExpectedResult.Failure)]
[CancelDuringRetry]
public class RetryBackoffCancellationTests
{
    private const string StateRecordingRunId = "TUNIT_CANCELLATION_STATE_RUN_ID";
    private static ITestExecution? _execution;

    [Test]
    public void CancelBetweenRetryAttempts()
    {
        _execution = TestContext.Current!.Execution;
        throw new InvalidOperationException("Trigger retry.");
    }

    [After(Class)]
    public static void RecordFinalState()
    {
        if (Environment.GetEnvironmentVariable(StateRecordingRunId) is not { Length: > 0 } runId)
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), nameof(RetryBackoffCancellationTests), runId);
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "FinalState.txt"),
            _execution?.Result?.State.ToString() ?? "No result");
    }
}

public sealed class CancelDuringRetryAttribute : RetryAttribute
{
    public CancelDuringRetryAttribute() : base(1)
    {
        BackoffMs = 5_000;
    }

    public override Task<bool> ShouldRetry(TestContext context, Exception exception, int currentRetryCount)
    {
        context.Execution.Cancel();
        return Task.FromResult(true);
    }
}

[EngineTest(ExpectedResult.Failure)]
[Retry(1)]
public class CancelDuringRetryTransitionTests
{
    private const string StateRecordingRunId = "TUNIT_CANCELLATION_STATE_RUN_ID";
    private static ITestExecution? _execution;

    [Test]
    public void FailBeforeCancellation()
    {
        _execution = TestContext.Current!.Execution;
        throw new InvalidOperationException("Trigger retry.");
    }

    [After(Test)]
    public void CancelAfterFailedAttempt() => TestContext.Current!.Execution.Cancel();

    [After(Class)]
    public static void RecordFinalState() => RecordState(nameof(CancelDuringRetryTransitionTests), _execution);

    private static void RecordState(string className, ITestExecution? execution)
    {
        if (Environment.GetEnvironmentVariable(StateRecordingRunId) is not { Length: > 0 } runId)
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), className, runId);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "FinalState.txt"), execution?.Result?.State.ToString() ?? "No result");
    }
}

[EngineTest(ExpectedResult.Failure)]
[CancelAndThrowDuringRetry]
public class CancelAndThrowDuringRetryTests
{
    private const string StateRecordingRunId = "TUNIT_CANCELLATION_STATE_RUN_ID";
    private static ITestExecution? _execution;

    [Test]
    public void FailBeforeRetryDecision()
    {
        _execution = TestContext.Current!.Execution;
        throw new InvalidOperationException("Trigger retry.");
    }

    [After(Class)]
    public static void RecordFinalState()
    {
        if (Environment.GetEnvironmentVariable(StateRecordingRunId) is not { Length: > 0 } runId)
        {
            return;
        }

        var directory = Path.Combine(Path.GetTempPath(), nameof(CancelAndThrowDuringRetryTests), runId);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "FinalState.txt"), _execution?.Result?.State.ToString() ?? "No result");
    }
}

public sealed class CancelAndThrowDuringRetryAttribute : RetryAttribute
{
    public CancelAndThrowDuringRetryAttribute() : base(1)
    {
    }

    public override Task<bool> ShouldRetry(TestContext context, Exception exception, int currentRetryCount)
    {
        context.Execution.Cancel();
        throw new InvalidOperationException("Retry decision failed after cancellation.");
    }
}
