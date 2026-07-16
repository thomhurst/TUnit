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
    /// <exception cref="TimeoutException">Thrown when the timeout elapses before task completion.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    public static async Task ExecuteWithTimeoutAsync(
        Func<CancellationToken, Task> taskFactory,
        TimeSpan timeout,
        CancellationTokenSource timeoutCts,
        CancellationToken externalToken,
        string? timeoutMessage = null)
    {
        // Set up cancellation detection BEFORE scheduling timeout to avoid race condition
        // where timeout fires before registration completes (with very small timeouts)
        var cancelledTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = timeoutCts.Token.Register(
            static state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(),
            cancelledTcs);

        // Now schedule the timeout - registration is guaranteed to catch it
        timeoutCts.CancelAfter(timeout);

        var executionTask = taskFactory(timeoutCts.Token);

        var winner = await Task.WhenAny(executionTask, cancelledTcs.Task).ConfigureAwait(false);

        if (winner == cancelledTcs.Task)
        {
            // Determine if it was external cancellation or timeout
            if (externalToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(externalToken);
            }

            // Timeout occurred - give the execution task a brief grace period to clean up
            try
            {
#if NET8_0_OR_GREATER
                await executionTask.WaitAsync(GracePeriod, CancellationToken.None).ConfigureAwait(false);
#else
                // Use cancellable delay to avoid leaked tasks when executionTask completes first
                using var graceCts = new CancellationTokenSource();
                var delayTask = Task.Delay(GracePeriod, graceCts.Token);
                var graceWinner = await Task.WhenAny(executionTask, delayTask).ConfigureAwait(false);
                if (graceWinner == executionTask)
                {
                    graceCts.Cancel();
                }
#endif
            }
            catch
            {
                // Ignore all exceptions - task was cancelled, we're just giving it time to clean up
            }

            // Even if task completed during grace period, timeout already elapsed so we throw
            var baseMessage = timeoutMessage ?? $"Operation timed out after {timeout}";
            var diagnosticMessage = TimeoutDiagnostics.BuildTimeoutDiagnosticsMessage(baseMessage, executionTask);
            throw new TimeoutException(diagnosticMessage);
        }

        await executionTask.ConfigureAwait(false);
    }
}
