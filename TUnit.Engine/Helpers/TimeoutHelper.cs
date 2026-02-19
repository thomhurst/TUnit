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
    /// Executes a ValueTask-returning operation with an optional timeout.
    /// On the fast path (no timeout), returns the ValueTask directly without Task allocation.
    /// </summary>
    public static ValueTask ExecuteWithTimeoutAsync(
        Func<CancellationToken, ValueTask> valueTaskFactory,
        TimeSpan? timeout,
        CancellationToken cancellationToken,
        string? timeoutMessage = null)
    {
        // Fast path: no timeout - return ValueTask directly (zero allocation, no state machine)
        if (!timeout.HasValue)
        {
            return valueTaskFactory(cancellationToken);
        }

        // Timeout path: convert to Task for WhenAny support
        return new ValueTask(ExecuteWithTimeoutCoreAsync(valueTaskFactory, timeout.Value, cancellationToken, timeoutMessage));
    }

    private static async Task ExecuteWithTimeoutCoreAsync(
        Func<CancellationToken, ValueTask> valueTaskFactory,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        string? timeoutMessage)
    {
        await ExecuteWithTimeoutAsync<bool>(
            async ct =>
            {
                await valueTaskFactory(ct).ConfigureAwait(false);
                return true;
            },
            timeout,
            cancellationToken,
            timeoutMessage).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a task with an optional timeout. If the timeout elapses before the task completes,
    /// control is returned to the caller immediately with a TimeoutException.
    /// </summary>
    /// <param name="taskFactory">Factory function that creates the task to execute.</param>
    /// <param name="timeout">Optional timeout duration. If null, no timeout is applied.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <param name="timeoutMessage">Optional custom timeout message. If null, uses default message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="TimeoutException">Thrown when the timeout elapses before task completion.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    public static async Task ExecuteWithTimeoutAsync(
        Func<CancellationToken, Task> taskFactory,
        TimeSpan? timeout,
        CancellationToken cancellationToken,
        string? timeoutMessage = null)
    {
        await ExecuteWithTimeoutAsync(
            async ct =>
            {
                await taskFactory(ct).ConfigureAwait(false);
                return true;
            },
            timeout,
            cancellationToken,
            timeoutMessage).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a task with an optional timeout and returns a result. If the timeout elapses before the task completes,
    /// control is returned to the caller immediately with a TimeoutException.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the task.</typeparam>
    /// <param name="taskFactory">Factory function that creates the task to execute.</param>
    /// <param name="timeout">Optional timeout duration. If null, no timeout is applied.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <param name="timeoutMessage">Optional custom timeout message. If null, uses default message.</param>
    /// <returns>The result of the completed task.</returns>
    /// <exception cref="TimeoutException">Thrown when the timeout elapses before task completion.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    public static async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> taskFactory,
        TimeSpan? timeout,
        CancellationToken cancellationToken,
        string? timeoutMessage = null)
    {
        // Fast path: no timeout specified
        if (!timeout.HasValue)
        {
#if NET6_0_OR_GREATER
            // Use WaitAsync to stop waiting immediately on cancellation while avoiding
            // TCS + CancellationTokenRegistration allocations. The task still runs to completion
            // but we return control to the caller immediately.
            return await taskFactory(cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
#else
            // On older frameworks, rely on cooperative cancellation
            return await taskFactory(cancellationToken).ConfigureAwait(false);
#endif
        }

        // Timeout path: create linked token so task can observe both timeout and external cancellation.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Set up cancellation detection BEFORE scheduling timeout to avoid race condition
        // where timeout fires before registration completes (with very small timeouts)
        var cancelledTcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = timeoutCts.Token.Register(
            static state => ((TaskCompletionSource<T>)state!).TrySetCanceled(),
            cancelledTcs);

        // Now schedule the timeout - registration is guaranteed to catch it
        timeoutCts.CancelAfter(timeout.Value);

        var executionTask = taskFactory(timeoutCts.Token);

        var winner = await Task.WhenAny(executionTask, cancelledTcs.Task).ConfigureAwait(false);

        if (winner == cancelledTcs.Task)
        {
            // Determine if it was external cancellation or timeout
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
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
            var baseMessage = timeoutMessage ?? $"Operation timed out after {timeout.Value}";
            var diagnosticMessage = TimeoutDiagnostics.BuildTimeoutDiagnosticsMessage(baseMessage, executionTask);
            throw new TimeoutException(diagnosticMessage);
        }

        return await executionTask.ConfigureAwait(false);
    }
}
