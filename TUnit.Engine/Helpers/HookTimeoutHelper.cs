using TUnit.Core.Hooks;
using TUnit.Core.Settings;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Helper class for executing hooks with timeout enforcement.
/// When no explicit timeout is set on a hook, falls back to
/// <see cref="TUnitSettings.Default"/>.<see cref="TUnitSettings.Timeouts"/>.<see cref="TimeoutSettings.DefaultHookTimeout"/>.
/// </summary>
internal static class HookTimeoutHelper
{
    /// <summary>
    /// Creates a timeout-aware action wrapper for a hook
    /// </summary>
    public static Task CreateTimeoutHookAction<T>(
        StaticHookMethod<T> hook,
        T context,
        CancellationToken cancellationToken)
    {
        var timeout = hook.Timeout ?? TUnitSettings.Default.Timeouts.DefaultHookTimeout;

        // Fast path: skip CTS allocation when timeout is effectively infinite
        if (timeout == Timeout.InfiniteTimeSpan || timeout >= TimeSpan.FromDays(1))
        {
            return hook.ExecuteAsync(context, cancellationToken).AsTask();
        }

        var timeoutMs = (int)timeout.TotalMilliseconds;

        return CreateTimeoutHookActionAsync(hook, context, timeoutMs, cancellationToken);

        static async Task CreateTimeoutHookActionAsync(
            StaticHookMethod<T> hook,
            T context,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            try
            {
                await hook.ExecuteAsync(context, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                var baseMessage = $"Hook '{hook.Name}' exceeded timeout of {timeoutMs}ms";
                throw new TimeoutException(TimeoutDiagnostics.BuildTimeoutDiagnosticsMessage(baseMessage, executionTask: null));
            }
        }
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
        var effectiveTimeout = timeout ?? TUnitSettings.Default.Timeouts.DefaultHookTimeout;

        // Fast path: skip CTS allocation when timeout is effectively infinite
        if (effectiveTimeout == Timeout.InfiniteTimeSpan || effectiveTimeout >= TimeSpan.FromDays(1))
        {
            return () => hookDelegate(context, cancellationToken);
        }

        var timeoutMs = (int)effectiveTimeout.TotalMilliseconds;

        return async () =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            try
            {
                await hookDelegate(context, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                var baseMessage = $"Hook '{hookName}' exceeded timeout of {timeoutMs}ms";
                throw new TimeoutException(TimeoutDiagnostics.BuildTimeoutDiagnosticsMessage(baseMessage, executionTask: null));
            }
        };
    }

    /// <summary>
    /// Creates a timeout-aware action wrapper for a hook delegate that returns ValueTask.
    /// This overload is used for instance hooks (InstanceHookMethod).
    /// Custom executor handling for instance hooks is done in HookDelegateBuilder.CreateInstanceHookDelegateAsync.
    /// </summary>
    public static Func<Task> CreateTimeoutHookAction<T>(
        Func<T, CancellationToken, ValueTask> hookDelegate,
        T context,
        TimeSpan? timeout,
        string hookName,
        CancellationToken cancellationToken)
    {
        var effectiveTimeout = timeout ?? TUnitSettings.Default.Timeouts.DefaultHookTimeout;

        // Fast path: skip CTS allocation when timeout is effectively infinite
        if (effectiveTimeout == Timeout.InfiniteTimeSpan || effectiveTimeout >= TimeSpan.FromDays(1))
        {
            return () => hookDelegate(context, cancellationToken).AsTask();
        }

        var timeoutMs = (int)effectiveTimeout.TotalMilliseconds;

        return async () =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            try
            {
                await hookDelegate(context, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                var baseMessage = $"Hook '{hookName}' exceeded timeout of {timeoutMs}ms";
                throw new TimeoutException(TimeoutDiagnostics.BuildTimeoutDiagnosticsMessage(baseMessage, executionTask: null));
            }
        };
    }
}
