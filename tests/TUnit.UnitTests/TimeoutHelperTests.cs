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

    [Test]
    public async Task Queued_Body_Does_Not_Consume_Its_Timeout()
    {
        using var timeoutCts = new CancellationTokenSource();
        var execution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = timeoutCts.Token.Register(() => execution.TrySetCanceled(timeoutCts.Token));
        Action? start = null;

        var timeoutTask = TimeoutHelper.ExecuteWithTimeoutFromStartAsync(
            (_, startTimeout) =>
            {
                // Model a queued invocation without starving the process-wide thread pool.
                start = startTimeout;
                return execution.Task;
            }, TimeSpan.FromMilliseconds(50), timeoutCts, CancellationToken.None);

        try
        {
            await Task.Delay(150);
            await Assert.That(timeoutCts.IsCancellationRequested).IsFalse();
            await Assert.That(timeoutTask.IsCompleted).IsFalse();
            start!();
            await Assert.That(() => timeoutTask!).ThrowsExactly<TimeoutException>();
        }
        finally
        {
            execution.TrySetResult();
        }
    }

    [Test]
    public async Task External_Cancellation_Remains_Active_While_Body_Is_Queued()
    {
        using var externalCts = new CancellationTokenSource();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(externalCts.Token);
        var execution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timeoutTask = TimeoutHelper.ExecuteWithTimeoutFromStartAsync(
            (_, _) => execution.Task, TimeSpan.FromMilliseconds(50), timeoutCts, externalCts.Token);

        externalCts.Cancel();
        var exception = await Assert.That(() => timeoutTask!).ThrowsExactly<OperationCanceledException>();
        await Assert.That(exception!.CancellationToken).IsEqualTo(externalCts.Token);

        // Even a late fault after external cancellation must be observed by the helper.
        execution.SetException(new InvalidOperationException("Fault after cancellation"));
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Late_Fault_After_Cancellation_Is_Observed(bool externalCancellation)
    {
        var message = Guid.NewGuid().ToString();
        var unobserved = false;
        void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            if (args.Exception.Flatten().InnerExceptions.Any(exception => exception.Message == message))
            {
                unobserved = true;
                args.SetObserved();
            }
        }

        TaskScheduler.UnobservedTaskException += OnUnobserved;
        try
        {
            var execution = await CreateLateFaultAsync(message, externalCancellation);
            // Completion continuations can still hold the task on another thread's stack.
            // Yield between collections until those frames unwind and the task is reclaimed.
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            while (execution.IsAlive && elapsed.Elapsed < TimeSpan.FromSeconds(5))
            {
                await Task.Delay(10);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            await Assert.That(execution.IsAlive).IsFalse();
            await Assert.That(unobserved).IsFalse();
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }
    }

    private static async Task<WeakReference> CreateLateFaultAsync(string message, bool externalCancellation)
    {
        using var externalCts = new CancellationTokenSource();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(externalCts.Token);
        TaskCompletionSource? execution = new();
        Task? timeoutTask = TimeoutHelper.ExecuteWithTimeoutAsync(
            _ => execution!.Task, Timeout.InfiniteTimeSpan, timeoutCts, externalCts.Token);

        if (externalCancellation)
        {
            externalCts.Cancel();
            await Assert.That(() => timeoutTask!).ThrowsExactly<OperationCanceledException>();
        }
        else
        {
            timeoutCts.Cancel();
            await Assert.That(() => timeoutTask!).ThrowsExactly<TimeoutException>();
        }

        var reference = new WeakReference(execution.Task);
        execution.SetException(new InvalidOperationException(message));
        // Do not keep the task alive through this method's completed async state machine.
        execution = null;
        timeoutTask = null;
        return reference;
    }

    [Test]
    public async Task Started_Synchronously_Blocking_Body_Times_Out()
    {
        using var timeoutCts = new CancellationTokenSource();
        using var release = new ManualResetEventSlim();
        var executionFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timeoutTask = TimeoutHelper.ExecuteWithTimeoutFromStartAsync(
            (token, startTimeout) => Task.Run(() =>
            {
                try
                {
                    startTimeout();
                    release.Wait(TimeSpan.FromSeconds(10));
                }
                finally
                {
                    executionFinished.SetResult();
                }
            }, token), TimeSpan.FromMilliseconds(50), timeoutCts, CancellationToken.None);

        try
        {
            await Assert.That(() => timeoutTask!).ThrowsExactly<TimeoutException>();
        }
        finally
        {
            release.Set();
            await executionFinished.Task;
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Ordinary_Factory_Has_Timeout_Armed_Before_Invocation(bool throwsSynchronously)
    {
        await Assert.That(() => TimeoutHelper.ExecuteWithTimeoutAsync(
                token =>
                {
                    // This factory does not return its task until the timeout is signalled.
                    // Starting the timer after the factory returned would miss this cancellation.
                    token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                    if (throwsSynchronously)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    return Task.FromCanceled(token);
                }, TimeSpan.FromMilliseconds(50), CancellationToken.None))
            .ThrowsExactly<TimeoutException>();
    }

    [Test]
    public async Task Synchronous_External_Cancellation_Preserves_External_Token()
    {
        using var externalCts = new CancellationTokenSource();
        var exception = await Assert.That(() => TimeoutHelper.ExecuteWithTimeoutAsync(
                token =>
                {
                    externalCts.Cancel();
                    token.ThrowIfCancellationRequested();
                    return Task.CompletedTask;
                }, Timeout.InfiniteTimeSpan, externalCts.Token))
            .ThrowsExactly<OperationCanceledException>();

        await Assert.That(exception!.CancellationToken).IsEqualTo(externalCts.Token);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Synchronous_Independent_Cancellation_Is_Preserved(bool tokenless)
    {
        var expected = tokenless
            ? new OperationCanceledException("Independent cancellation")
            : new OperationCanceledException(new CancellationToken(true));

        var exception = await Assert.That(() => ExecuteWithControlledTimeoutAsync(
                _ => throw expected, executionCompletesFirst: true))
            .ThrowsExactly<OperationCanceledException>();

        await Assert.That(exception).IsSameReferenceAs(expected);
    }

    [Test]
    public async Task Synchronous_Timeout_Cancellation_Preserves_Diagnostics()
    {
        OperationCanceledException? expected = null;
        var exception = await Assert.That(() => ExecuteWithControlledTimeoutAsync(
                token =>
                {
                    expected = new OperationCanceledException("Timeout diagnostic", token);
                    throw expected;
                },
                executionCompletesFirst: true))
            .ThrowsExactly<TimeoutException>();

        await Assert.That(exception!.InnerException).IsSameReferenceAs(expected);
        await Assert.That(exception.Message).Contains("Timeout diagnostic");
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
        // Continuations run inline so that SetResult below completes the released operation on this
        // thread. Queuing them would let a busy thread pool push the operation past the grace period,
        // at which point TimeoutHelper stops waiting and the preserved exception is lost (#7172).
        var releaseOperation = new TaskCompletionSource();
        Task? releasedExecution = null;

        var timeoutTask = TimeoutHelper.ExecuteWithTimeoutAsync(
            cancellationToken =>
            {
                testContext?.SetCancellationToken(cancellationToken);
                // Cancel the caller-owned timeout source without depending on a timer or scheduler delay.
                timeoutCts.Cancel();
                return executionCompletesFirst
                    ? operation(cancellationToken)
                    : releasedExecution = CompleteAfterReleaseAsync(cancellationToken);
            }, Timeout.InfiniteTimeSpan, timeoutCts, CancellationToken.None, testContext: testContext);

        // When both tasks are already complete, WhenAny selects execution (the first argument).
        // Otherwise timeout detection wins before the operation is released into the grace period.
        releaseOperation.SetResult();

        if (!executionCompletesFirst && releasedExecution is not { IsCompleted: true })
        {
            throw new InvalidOperationException(
                "The released operation must be created synchronously by TimeoutHelper and complete synchronously on release so the grace period observes its outcome.");
        }

        await timeoutTask;
        return;

        async Task CompleteAfterReleaseAsync(CancellationToken cancellationToken)
        {
            await releaseOperation.Task.ConfigureAwait(false);
            await operation(cancellationToken).ConfigureAwait(false);
        }
    }
}
