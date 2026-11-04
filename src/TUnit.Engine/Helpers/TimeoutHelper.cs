using TUnit.Core;
using TUnit.Engine.Constants;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Reusable utility for executing tasks with timeout support that can return control immediately when timeout elapses.
/// </summary>
internal static class TimeoutHelper
{
    /// <summary>
    /// Grace period to allow tasks to handle cancellation before throwing timeout exception.
    /// </summary>
    private static readonly TimeSpan GracePeriod = EngineDefaults.TimeoutGracePeriod;

    /// <summary>
    /// Executes a task with a timeout, owning the linked cancellation source for the duration of
    /// the call. If the timeout elapses before the task completes, control is returned to the
    /// caller immediately with a TimeoutException.
    /// </summary>
    /// <param name="taskFactory">Factory function that creates the task to execute.</param>
    /// <param name="timeout">Timeout duration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <param name="timeoutMessage">Optional custom timeout message. If null, uses default message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="TimeoutException">Thrown when the timeout elapses before task completion.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    public static async Task ExecuteWithTimeoutAsync(
        Func<CancellationToken, Task> taskFactory,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        string? timeoutMessage = null)
    {
        // Timeout path: create linked token so task can observe both timeout and external cancellation.
        // Standalone callers have nothing observing the token after this returns, so we own its
        // lifetime here. Callers that hand the token to code which may touch it after the body
        // returns (e.g. TestExecutor's After-hook phase) must use the CTS-owning overload instead.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await ExecuteWithTimeoutAsync(taskFactory, timeout, timeoutCts, cancellationToken, timeoutMessage).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a task with a timeout using a <b>caller-owned</b> linked cancellation source.
    /// <para>
    /// The caller is responsible for disposing <paramref name="timeoutCts"/>, and must keep it alive
    /// until every consumer that may have captured <c>timeoutCts.Token</c> has finished — this includes
    /// the test body <b>and</b> any teardown (After-hook / test-end) phase that runs afterwards.
    /// Disposing the source while a captured token copy is still reachable makes a synchronous
    /// <see cref="CancellationToken.WaitHandle"/> wait (EF Core / Respawn / SemaphoreSlim / host shutdown)
    /// throw <see cref="ObjectDisposedException"/> "The CancellationTokenSource has been disposed." — the
    /// randomly-surfacing After(Test) failure in issue #6339.
    /// </para>
    /// </summary>
    /// <param name="taskFactory">Factory function that creates the task to execute.</param>
    /// <param name="timeout">Timeout duration.</param>
    /// <param name="timeoutCts">Caller-owned CTS, already linked to <paramref name="externalToken"/>. Not disposed here.</param>
    /// <param name="externalToken">The external token the CTS is linked to; used to distinguish timeout from external cancellation.</param>
    /// <param name="timeoutMessage">Optional custom timeout message. If null, uses default message.</param>
    /// <param name="testContext">Tracks execution tokens linked to the timeout token, including captured tokens replaced during execution.</param>
    /// <exception cref="TimeoutException">Thrown when the timeout elapses before task completion.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    public static Task ExecuteWithTimeoutAsync(
        Func<CancellationToken, Task> taskFactory,
        TimeSpan timeout,
        CancellationTokenSource timeoutCts,
        CancellationToken externalToken,
        string? timeoutMessage = null,
        TestContext? testContext = null)
        => ExecuteWithTimeoutCoreAsync(taskFactory, timeout, timeoutCts, externalToken,
            timeoutMessage, testContext, startTimeoutImmediately: true);

    /// <summary>
    /// Executes a scheduled task with external cancellation active while queued. The factory must
    /// call the supplied start action immediately before invoking the body, so queue delay does not
    /// consume the body's timeout. The caller owns the linked cancellation source.
    /// </summary>
    public static Task ExecuteWithTimeoutFromStartAsync(
        Func<CancellationToken, Action, Task> taskFactory,
        TimeSpan timeout,
        CancellationTokenSource timeoutCts,
        CancellationToken externalToken,
        string? timeoutMessage = null,
        TestContext? testContext = null)
        => ExecuteWithTimeoutCoreAsync(
            token => taskFactory(token, () => timeoutCts.CancelAfter(timeout)),
            timeout, timeoutCts, externalToken, timeoutMessage, testContext, startTimeoutImmediately: false);

    private static async Task ExecuteWithTimeoutCoreAsync(
        Func<CancellationToken, Task> taskFactory,
        TimeSpan timeout,
        CancellationTokenSource timeoutCts,
        CancellationToken externalToken,
        string? timeoutMessage,
        TestContext? testContext,
        bool startTimeoutImmediately)
    {
        // Set up cancellation detection BEFORE scheduling timeout to avoid race condition
        // where timeout fires before registration completes (with very small timeouts)
        var cancelledTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = timeoutCts.Token.Register(
            static state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(),
            cancelledTcs);

        // Ordinary factories may block synchronously, so arm their timeout before calling them.
        // Scheduled factories instead signal when the body starts; external cancellation is already active.
        if (startTimeoutImmediately)
        {
            timeoutCts.CancelAfter(timeout);
        }

        Task executionTask;
        try
        {
            executionTask = taskFactory(timeoutCts.Token);
        }
        catch (OperationCanceledException exception)
            when (timeoutCts.IsCancellationRequested
                && IsTimeoutCancellationToken(exception.CancellationToken, timeoutCts.Token, testContext))
        {
            // A factory can observe cancellation before returning a task. Represent routine
            // cancellation as canceled so task diagnostics cannot reintroduce its exception.
            // Keep custom exceptions faulted to preserve their diagnostic details.
            executionTask = IsRoutineCancellation(exception, timeoutCts.Token, testContext)
                ? Task.FromCanceled(timeoutCts.Token)
                : Task.FromException(exception);
        }

        var winner = await Task.WhenAny(executionTask, cancelledTcs.Task).ConfigureAwait(false);

        if (winner == executionTask)
        {
            try
            {
                await executionTask.ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException exception)
                when (timeoutCts.IsCancellationRequested
                    && IsTimeoutCancellationToken(exception.CancellationToken, timeoutCts.Token, testContext))
            {
                // The operation can observe cancellation before the detection task wins WhenAny.
                // Classify that cancellation through the same timeout/external-cancellation path.
            }
        }

        // The body can fault after cancellation and even after the grace period. Observe that
        // eventual fault on every target framework, including the external-cancellation path.
        _ = executionTask.ContinueWith(static task => { _ = task.Exception; },
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        // Determine if it was external cancellation or timeout
        if (externalToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(externalToken);
        }

        // Timeout occurred - give the execution task a brief grace period to clean up
        var executionException = await ObserveExceptionDuringGracePeriodAsync(executionTask).ConfigureAwait(false);

        // Routine cancellation adds no useful context; preserve exceptions explicitly
        // thrown while handling cancellation, such as Aspire's diagnostic exception.
        var exceptionToPreserve = IsRoutineCancellation(executionException, timeoutCts.Token, testContext)
            ? null
            : executionException;

        // Even if task completed during grace period, timeout already elapsed so we throw
        var baseMessage = timeoutMessage ?? $"Operation timed out after {timeout}";
        var diagnosticMessage = TimeoutDiagnostics.BuildTimeoutDiagnosticsMessage(baseMessage, executionTask, exceptionToPreserve);
        throw new TimeoutException(diagnosticMessage, exceptionToPreserve);
    }

    private static bool IsTimeoutCancellationToken(CancellationToken token, CancellationToken timeoutToken, TestContext? testContext)
        => token == timeoutToken || testContext?.IsLinkedCancellationToken(token, timeoutToken) == true;

    private static bool IsRoutineCancellation(Exception? exception, CancellationToken timeoutToken, TestContext? testContext)
    {
        if (exception is not OperationCanceledException
            {
                InnerException: null
            } operationCanceledException
            || !IsTimeoutCancellationToken(operationCanceledException.CancellationToken, timeoutToken, testContext))
        {
            return false;
        }

        return operationCanceledException switch
        {
            TaskCanceledException taskCanceledException
                when taskCanceledException.GetType() == typeof(TaskCanceledException) =>
                taskCanceledException.Message == new TaskCanceledException().Message,
            { } when operationCanceledException.GetType() == typeof(OperationCanceledException) =>
                operationCanceledException.Message == new OperationCanceledException(timeoutToken).Message,
            _ => false
        };
    }

    private static async Task<Exception?> ObserveExceptionDuringGracePeriodAsync(Task executionTask)
    {
#if NET8_0_OR_GREATER
        try
        {
            await executionTask.WaitAsync(GracePeriod, CancellationToken.None).ConfigureAwait(false);
            return null;
        }
        catch (TimeoutException)
        {
            return executionTask.IsCompleted
                ? await ObserveCompletedTaskExceptionAsync(executionTask).ConfigureAwait(false)
                : null;
        }
        catch (Exception ex)
        {
            return ex;
        }
#else
        // Use cancellable delay to avoid leaked tasks when executionTask completes first
        using var graceCts = new CancellationTokenSource();
        var delayTask = Task.Delay(GracePeriod, graceCts.Token);
        if (await Task.WhenAny(executionTask, delayTask).ConfigureAwait(false) != executionTask)
        {
            return null;
        }

        graceCts.Cancel();
        return await ObserveCompletedTaskExceptionAsync(executionTask).ConfigureAwait(false);
#endif
    }

    private static async Task<Exception?> ObserveCompletedTaskExceptionAsync(Task executionTask)
    {
        try
        {
            await executionTask.ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
