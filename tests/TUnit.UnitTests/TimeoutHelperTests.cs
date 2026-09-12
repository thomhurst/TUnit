using TUnit.Core;
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
    public async Task Independent_Operation_Cancellation_Is_Preserved_When_Timeout_Is_Also_Cancelled()
    {
        var operationToken = new CancellationToken(true);

        var exception = await Assert.That(() => ExecuteWithControlledTimeoutAsync(
                _ => Task.FromCanceled(operationToken), executionCompletesFirst: true))
            .ThrowsExactly<TaskCanceledException>();

        await Assert.That(exception!.CancellationToken).IsEqualTo(operationToken);
    }

    [Test]
    public async Task Tokenless_Operation_Cancellation_Is_Preserved_When_Timeout_Is_Also_Cancelled()
    {
        var expected = new OperationCanceledException("Independent cancellation");

        var exception = await Assert.That(() => ExecuteWithControlledTimeoutAsync(
                _ => Task.FromException(expected), executionCompletesFirst: true))
            .ThrowsExactly<OperationCanceledException>();

        await Assert.That(exception).IsSameReferenceAs(expected);
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

    [Test]
    [Arguments(true, false)]
    [Arguments(true, true)]
    [Arguments(false, false)]
    [Arguments(false, true)]
    public async Task Linked_Timeout_Cancellation_Is_Classified_Without_Routine_Diagnostics(
        bool executionCompletesFirst, bool rebuildAfterCapture)
    {
        var context = CreateContext();
        context.Execution.AddLinkedCancellationToken(CancellationToken.None);

        try
        {
            var exception = await Assert.That(() => ExecuteWithControlledTimeoutAsync(
                    _ =>
                    {
                        var capturedToken = context.Execution.CancellationToken;
                        if (rebuildAfterCapture)
                        {
                            context.Execution.AddLinkedCancellationToken(CancellationToken.None);
                        }

                        return Task.FromCanceled(capturedToken);
                    }, executionCompletesFirst, context))
                .ThrowsExactly<TimeoutException>();

            await Assert.That(exception!.InnerException).IsNull();
            await Assert.That(exception.Message).DoesNotContain(nameof(TaskCanceledException));
        }
        finally
        {
            context.DisposeLinkedCancellationTokenSources();
            context.RemoveFromRegistry();
        }
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Linked_Timeout_Preserves_Custom_Diagnostics(bool executionCompletesFirst)
    {
        var context = CreateContext();
        context.Execution.AddLinkedCancellationToken(CancellationToken.None);
        OperationCanceledException? expected = null;

        try
        {
            var exception = await Assert.That(() => ExecuteWithControlledTimeoutAsync(
                    _ =>
                    {
                        expected = new OperationCanceledException("Linked timeout diagnostic", context.Execution.CancellationToken);
                        return Task.FromException(expected);
                    }, executionCompletesFirst, context))
                .ThrowsExactly<TimeoutException>();

            await Assert.That(exception!.InnerException).IsSameReferenceAs(expected);
            await Assert.That(exception.Message).Contains("Linked timeout diagnostic");
        }
        finally
        {
            context.DisposeLinkedCancellationTokenSources();
            context.RemoveFromRegistry();
        }
    }

    [Test]
    public async Task Linked_Cancellation_From_An_Earlier_Base_Is_Not_The_Current_Timeout()
    {
        var context = CreateContext();
        using var earlierSource = new CancellationTokenSource();
        context.SetCancellationToken(earlierSource.Token);
        context.Execution.AddLinkedCancellationToken(CancellationToken.None);
        var earlierToken = context.Execution.CancellationToken;
        earlierSource.Cancel();

        try
        {
            var exception = await Assert.That(() => ExecuteWithControlledTimeoutAsync(
                    _ => Task.FromCanceled(earlierToken), executionCompletesFirst: true, context))
                .ThrowsExactly<TaskCanceledException>();

            await Assert.That(exception!.CancellationToken).IsEqualTo(earlierToken);
        }
        finally
        {
            context.DisposeLinkedCancellationTokenSources();
            context.RemoveFromRegistry();
        }
    }

    private static TestContext CreateContext()
    {
        var currentContext = TestContext.Current!;
        return new TestContext(nameof(TimeoutHelperTests), currentContext.ServiceProvider, currentContext.ClassContext,
            new TestBuilderContext { TestMetadata = currentContext.TestDetails.MethodMetadata }, CancellationToken.None);
    }

    private static async Task ExecuteWithControlledTimeoutAsync(
        Func<CancellationToken, Task> operation, bool executionCompletesFirst, TestContext? testContext = null)
    {
        using var timeoutCts = new CancellationTokenSource();
        var releaseOperation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var timeoutTask = TimeoutHelper.ExecuteWithTimeoutAsync(
            cancellationToken =>
            {
                testContext?.SetCancellationToken(cancellationToken);
                // Cancel the caller-owned timeout source without depending on a timer or scheduler delay.
                timeoutCts.Cancel();
                return executionCompletesFirst ? operation(cancellationToken) : CompleteAfterReleaseAsync(cancellationToken);
            }, Timeout.InfiniteTimeSpan, timeoutCts, CancellationToken.None, testContext: testContext);

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
