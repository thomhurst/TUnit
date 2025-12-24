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
    public static async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> taskFactory,
        TimeSpan? timeout,
        CancellationToken cancellationToken,
        string? timeoutMessage = null)
    {
        var executionTask = taskFactory(cancellationToken);

        // Create a task that completes when cancellation is requested
        var tcs = new TaskCompletionSource<bool>();
        using var registration = cancellationToken.Register(() => tcs.TrySetResult(true));

        // Create timeout task if timeout is specified
        using var timeoutCts = new CancellationTokenSource();
        var timeoutTask = timeout.HasValue
            ? Task.Delay(timeout.Value, timeoutCts.Token)
            : Task.Delay(Timeout.Infinite, timeoutCts.Token);

        var winningTask = await Task.WhenAny(executionTask, tcs.Task, timeoutTask).ConfigureAwait(false);

        // Cancellation requested
        if (winningTask == tcs.Task)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        // Timeout occurred
        if (winningTask == timeoutTask)
        {
            // Give the execution task a chance to handle cancellation gracefully
            try
            {
                await executionTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is properly handled
            }
            catch
            {
                // Ignore other exceptions from the cancelled task
            }

            var message = timeoutMessage ?? $"Operation timed out after {timeout!.Value}";
            throw new TimeoutException(message);
        }

        // Task completed normally - cancel the timeout task to free resources
        timeoutCts.Cancel();

        return await executionTask.ConfigureAwait(false);
    }
}
