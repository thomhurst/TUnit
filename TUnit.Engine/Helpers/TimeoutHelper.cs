namespace TUnit.Engine.Helpers;

/// <summary>
/// Reusable utility for executing tasks with timeout support that can return control immediately when timeout elapses.
/// </summary>
internal static class TimeoutHelper
{
    /// <summary>
    /// Grace period to allow tasks to handle cancellation before throwing timeout exception.
    /// </summary>
    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(1);

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
            var task = taskFactory(cancellationToken);

            // If the token can't be cancelled, just await directly (avoid allocations)
            if (!cancellationToken.CanBeCanceled)
            {
                return await task.ConfigureAwait(false);
            }

            // Race against cancellation - TrySetCanceled makes the TCS throw OperationCanceledException when awaited
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var reg = cancellationToken.Register(
                static state => ((TaskCompletionSource<T>)state!).TrySetCanceled(),
                tcs);

            // await await: first gets winning task, then awaits it (propagates result or exception)
            return await await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
        }

        // Timeout path: create linked token so task can observe both timeout and external cancellation.
        // CancelAfter schedules automatic cancellation - no need for separate Task.Delay timer.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout.Value);

        var executionTask = taskFactory(timeoutCts.Token);

        // TCS fires when linked token is cancelled (either external cancellation OR timeout)
        var cancelledTcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = timeoutCts.Token.Register(
            static state => ((TaskCompletionSource<T>)state!).TrySetCanceled(),
            cancelledTcs);

        var winner = await Task.WhenAny(executionTask, cancelledTcs.Task).ConfigureAwait(false);

        if (winner == cancelledTcs.Task)
        {
            // Determine if it was external cancellation or timeout
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            // Timeout occurred - give the execution task a brief grace period
            try
            {
#if NET8_0_OR_GREATER
                await executionTask.WaitAsync(GracePeriod, CancellationToken.None).ConfigureAwait(false);
#else
                await Task.WhenAny(executionTask, Task.Delay(GracePeriod, CancellationToken.None)).ConfigureAwait(false);
#endif
            }
            catch
            {
                // Ignore all exceptions from the cancelled task
            }

            throw new TimeoutException(timeoutMessage ?? $"Operation timed out after {timeout.Value}");
        }

        return await executionTask.ConfigureAwait(false);
    }
}
