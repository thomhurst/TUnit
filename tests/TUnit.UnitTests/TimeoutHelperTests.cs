using TUnit.Engine.Helpers;

namespace TUnit.UnitTests;

public class TimeoutHelperTests
{
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Timeout_Preserves_Exception_Thrown_During_Cancellation(bool executionCompletesFirst)
    {
        const string cancellationMessage = "Failed due to XYZ";

        var exception = await Assert.That(() => ExecuteWithControlledTimeoutAsync(
                async cancellationToken =>
                {
                    try
                    {
                        await Task.FromCanceled(cancellationToken);
                    }
                    catch (OperationCanceledException ex)
                    {
                        throw new OperationCanceledException(cancellationMessage, ex.CancellationToken);
                    }
                }, executionCompletesFirst))
            .ThrowsExactly<TimeoutException>();

        await Assert.That(exception!.Message).Contains(cancellationMessage);
        await Assert.That(exception.InnerException).IsTypeOf<OperationCanceledException>();
        await Assert.That(exception.InnerException!.Message).IsEqualTo(cancellationMessage);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Timeout_Does_Not_Preserve_Routine_Operation_Cancellation(bool executionCompletesFirst)
    {
        var exception = await Assert.That(() => ExecuteWithControlledTimeoutAsync(
                async cancellationToken =>
                {
                    try
                    {
                        await Task.FromCanceled(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }, executionCompletesFirst))
            .ThrowsExactly<TimeoutException>();

        await Assert.That(exception!.InnerException).IsNull();
        await Assert.That(exception.Message).DoesNotContain(nameof(OperationCanceledException));
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Timeout_Preserves_Custom_Task_Cancellation(bool executionCompletesFirst)
    {
        const string cancellationMessage = "Custom task cancellation diagnostic";
        var diagnosticException = new InvalidOperationException("Inner diagnostic");

        var exception = await Assert.That(() => ExecuteWithControlledTimeoutAsync(
                async cancellationToken =>
                {
                    try
                    {
                        await Task.FromCanceled(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw new TaskCanceledException(cancellationMessage, diagnosticException, cancellationToken);
                    }
                }, executionCompletesFirst))
            .ThrowsExactly<TimeoutException>();

        var taskCanceledException = await Assert.That(exception!.InnerException).IsTypeOf<TaskCanceledException>();
        await Assert.That(taskCanceledException!.Message).IsEqualTo(cancellationMessage);
        await Assert.That(taskCanceledException.InnerException).IsSameReferenceAs(diagnosticException);
    }

    [Test]
    public async Task External_Cancellation_Preserves_External_Token_When_Execution_Completes_First()
    {
        using var externalCts = new CancellationTokenSource();

        var exception = await Assert.That(() => TimeoutHelper.ExecuteWithTimeoutAsync(
                cancellationToken =>
                {
                    externalCts.Cancel();
                    return Task.FromCanceled(cancellationToken);
                }, Timeout.InfiniteTimeSpan, externalCts.Token))
            .ThrowsExactly<OperationCanceledException>();

        await Assert.That(exception!.CancellationToken).IsEqualTo(externalCts.Token);
    }

    [Test]
    public async Task Independent_Operation_Cancellation_Is_Not_A_Timeout()
    {
        var operationToken = new CancellationToken(true);

        var exception = await Assert.That(() => TimeoutHelper.ExecuteWithTimeoutAsync(
                _ => Task.FromCanceled(operationToken), Timeout.InfiniteTimeSpan, CancellationToken.None))
            .ThrowsExactly<TaskCanceledException>();

        await Assert.That(exception!.CancellationToken).IsEqualTo(operationToken);
    }

    [Test]
    public async Task Operation_Failure_Is_Preserved_Before_Timeout()
    {
        var expected = new InvalidOperationException("Operation failed");

        var exception = await Assert.That(() => TimeoutHelper.ExecuteWithTimeoutAsync(
                _ => Task.FromException(expected), Timeout.InfiniteTimeSpan, CancellationToken.None))
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(exception).IsSameReferenceAs(expected);
    }

    [Test]
    public async Task Operation_Can_Complete_Before_Timeout()
    {
        await TimeoutHelper.ExecuteWithTimeoutAsync(
            _ => Task.CompletedTask, Timeout.InfiniteTimeSpan, CancellationToken.None);
    }

    private static async Task ExecuteWithControlledTimeoutAsync(
        Func<CancellationToken, Task> operation, bool executionCompletesFirst)
    {
        using var timeoutCts = new CancellationTokenSource();
        var releaseOperation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var timeoutTask = TimeoutHelper.ExecuteWithTimeoutAsync(
            cancellationToken =>
            {
                // Cancel the caller-owned timeout source without depending on a timer or scheduler delay.
                timeoutCts.Cancel();
                return executionCompletesFirst ? operation(cancellationToken) : CompleteAfterReleaseAsync(cancellationToken);
            }, Timeout.InfiniteTimeSpan, timeoutCts, CancellationToken.None);

        // When both tasks are already complete, WhenAny selects execution (the first argument).
        // Otherwise timeout detection wins before the operation is released into the grace period.
        releaseOperation.SetResult();
        await timeoutTask;
        return;

        async Task CompleteAfterReleaseAsync(CancellationToken cancellationToken)
        {
            await releaseOperation.Task;
            await operation(cancellationToken);
        }
    }
}
