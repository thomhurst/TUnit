using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using TUnit.Core;

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
    /// <param name="taskFactory">Factory function that creates the task to execute</param>
    /// <param name="timeout">Optional timeout duration. If null, no timeout is applied</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <param name="timeoutMessage">Optional custom timeout message. If null, uses default message</param>
    /// <param name="timeProvider">Optional TimeProvider for controlling time. If null, uses TimeProvider.System</param>
    /// <returns>The completed task</returns>
    /// <exception cref="TimeoutException">Thrown when the timeout elapses before task completion</exception>
    public static async Task ExecuteWithTimeoutAsync(
        Func<CancellationToken, Task> taskFactory,
        TimeSpan? timeout,
        CancellationToken cancellationToken,
        string? timeoutMessage = null,
        TimeProvider? timeProvider = null)
    {
        if (!timeout.HasValue)
        {
            await taskFactory(cancellationToken).ConfigureAwait(false);
            return;
        }

        timeProvider ??= TimeProvider.System;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout.Value);

        var executionTask = taskFactory(timeoutCts.Token);

        // Use a cancellable timeout task to avoid leaving Task.Delay running in the background
        using var timeoutTaskCts = new CancellationTokenSource();
        var timeoutTask = timeProvider.Delay(timeout.Value, timeoutTaskCts.Token);

        var completedTask = await Task.WhenAny(executionTask, timeoutTask).ConfigureAwait(false);

        if (completedTask == timeoutTask)
        {
            timeoutCts.Cancel();

            try
            {
                await executionTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }

            var message = timeoutMessage ?? $"Operation timed out after {timeout.Value}";
            throw new TimeoutException(message);
        }

        // Task completed normally - cancel the timeout task to free resources immediately
        timeoutTaskCts.Cancel();

        // Await the result to propagate any exceptions
        await executionTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a task with an optional timeout and returns a result. If the timeout elapses before the task completes,
    /// control is returned to the caller immediately with a TimeoutException.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the task</typeparam>
    /// <param name="taskFactory">Factory function that creates the task to execute</param>
    /// <param name="timeout">Optional timeout duration. If null, no timeout is applied</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <param name="timeoutMessage">Optional custom timeout message. If null, uses default message</param>
    /// <param name="timeProvider">Optional TimeProvider for controlling time. If null, uses TimeProvider.System</param>
    /// <returns>The result of the completed task</returns>
    /// <exception cref="TimeoutException">Thrown when the timeout elapses before task completion</exception>
    public static async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> taskFactory,
        TimeSpan? timeout,
        CancellationToken cancellationToken,
        string? timeoutMessage = null,
        TimeProvider? timeProvider = null)
    {
        if (!timeout.HasValue)
        {
            return await taskFactory(cancellationToken).ConfigureAwait(false);
        }

        timeProvider ??= TimeProvider.System;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout.Value);

        var executionTask = taskFactory(timeoutCts.Token);

        // Use a cancellable timeout task to avoid leaving Task.Delay running in the background
        using var timeoutTaskCts = new CancellationTokenSource();
        var timeoutTask = timeProvider.Delay(timeout.Value, timeoutTaskCts.Token);

        var completedTask = await Task.WhenAny(executionTask, timeoutTask).ConfigureAwait(false);

        if (completedTask == timeoutTask)
        {
            timeoutCts.Cancel();

            try
            {
                await executionTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }

            var message = timeoutMessage ?? $"Operation timed out after {timeout.Value}";
            throw new TimeoutException(message);
        }

        // Task completed normally - cancel the timeout task to free resources immediately
        timeoutTaskCts.Cancel();

        // Await the result to propagate any exceptions
        return await executionTask.ConfigureAwait(false);
    }
}