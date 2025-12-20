namespace TUnit.Engine.Helpers;

/// <summary>
/// Extension methods for Task that provide cancellation-aware waiting.
/// </summary>
internal static class TaskExtensions
{
    /// <summary>
    /// Waits for a task to complete while observing a cancellation token.
    /// If the cancellation token is triggered before the task completes, throws OperationCanceledException.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A task that completes when either the original task completes or cancellation is requested.</returns>
    public static async Task WaitWithCancellationAsync(this Task task, CancellationToken cancellationToken)
    {
        if (task.IsCompleted)
        {
            await task.ConfigureAwait(false);
            return;
        }

        if (!cancellationToken.CanBeCanceled)
        {
            await task.ConfigureAwait(false);
            return;
        }

#if NET6_0_OR_GREATER
        await task.WaitAsync(cancellationToken).ConfigureAwait(false);
#else
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).TrySetCanceled(cancellationToken), tcs))
        {
            var completedTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
            if (completedTask == tcs.Task)
            {
                // Cancellation was requested - await to throw OperationCanceledException with correct token
                await tcs.Task.ConfigureAwait(false);
            }
            
            // Original task completed - propagate any exceptions
            await task.ConfigureAwait(false);
        }
#endif
    }
}
