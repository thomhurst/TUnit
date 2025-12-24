namespace TUnit.Engine.Helpers;

/// <summary>
/// Reusable utility for executing tasks with timeout support that can return control immediately when timeout elapses.
/// </summary>
internal static class TimeoutHelper
{
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

            // Race against cancellation to respond immediately when cancelled
            var tcs = new TaskCompletionSource<bool>();
            using var registration = cancellationToken.Register(() => tcs.TrySetResult(true));

            var completedTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);

            if (completedTask == tcs.Task)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return await task.ConfigureAwait(false);
        }

        // Timeout path: create linked token so task can observe both timeout and external cancellation
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout.Value);

        var executionTask = taskFactory(timeoutCts.Token);

        // Create a task that completes when cancellation is requested (for reactive cancellation)
        var cancellationTcs = new TaskCompletionSource<bool>();
        using var cancellationRegistration = cancellationToken.Register(() => cancellationTcs.TrySetResult(true));

        // Create timeout task
        using var timeoutTaskCts = new CancellationTokenSource();
        var timeoutTask = Task.Delay(timeout.Value, timeoutTaskCts.Token);

        var winner = await Task.WhenAny(executionTask, cancellationTcs.Task, timeoutTask).ConfigureAwait(false);

        // External cancellation requested
        if (winner == cancellationTcs.Task)
        {
            timeoutTaskCts.Cancel();
            throw new OperationCanceledException(cancellationToken);
        }

        // Timeout occurred
        if (winner == timeoutTask)
        {
            // Give the execution task a brief grace period to handle cancellation
            try
            {
#if NET8_0_OR_GREATER
                await executionTask.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None).ConfigureAwait(false);
#else
                var graceTask = Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
                await Task.WhenAny(executionTask, graceTask).ConfigureAwait(false);
#endif
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is properly handled
            }
            catch (TimeoutException)
            {
                // Grace period expired, which is fine
            }
            catch
            {
                // Ignore other exceptions from the cancelled task
            }

            var message = timeoutMessage ?? $"Operation timed out after {timeout.Value}";
            throw new TimeoutException(message);
        }

        // Task completed normally - cancel the timeout task to free resources
        timeoutTaskCts.Cancel();

        return await executionTask.ConfigureAwait(false);
    }
}
