using TUnit.Core.Hooks;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Helper class for executing hooks with timeout enforcement
/// </summary>
internal static class HookTimeoutHelper
{
    /// <summary>
    /// Creates a timeout-aware action wrapper for a hook
    /// </summary>
    public static Func<Task> CreateTimeoutHookAction<T>(
        StaticHookMethod<T> hook,
        T context,
        CancellationToken cancellationToken)
    {
        var timeout = hook.Timeout;
        if (timeout == null)
        {
            // No timeout specified, execute normally
            return async () => await hook.ExecuteAsync(context, cancellationToken);
        }

        return async () =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutMs = (int)timeout.Value.TotalMilliseconds;
            cts.CancelAfter(timeoutMs);

            try
            {
                await hook.ExecuteAsync(context, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new System.TimeoutException($"Hook '{hook.Name}' exceeded timeout of {timeoutMs}ms");
            }
        };
    }

    /// <summary>
    /// Creates a timeout-aware action wrapper for a hook delegate
    /// </summary>
    public static Func<Task> CreateTimeoutHookAction<T>(
        Func<T, CancellationToken, Task> hookDelegate,
        T context,
        TimeSpan? timeout,
        string hookName,
        CancellationToken cancellationToken)
    {
        if (timeout == null)
        {
            // No timeout specified, execute normally
            return async () => await hookDelegate(context, cancellationToken);
        }

        return async () =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutMs = (int)timeout.Value.TotalMilliseconds;
            cts.CancelAfter(timeoutMs);

            try
            {
                await hookDelegate(context, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new System.TimeoutException($"Hook '{hookName}' exceeded timeout of {timeoutMs}ms");
            }
        };
    }

    /// <summary>
    /// Creates a timeout-aware action wrapper for a hook delegate that returns ValueTask
    /// </summary>
    public static Func<Task> CreateTimeoutHookAction<T>(
        Func<T, CancellationToken, ValueTask> hookDelegate,
        T context,
        TimeSpan? timeout,
        string hookName,
        CancellationToken cancellationToken)
    {
        if (timeout == null)
        {
            // No timeout specified, execute normally
            return async () => await hookDelegate(context, cancellationToken);
        }

        return async () =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutMs = (int)timeout.Value.TotalMilliseconds;
            cts.CancelAfter(timeoutMs);

            try
            {
                await hookDelegate(context, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new System.TimeoutException($"Hook '{hookName}' exceeded timeout of {timeoutMs}ms");
            }
        };
    }
}