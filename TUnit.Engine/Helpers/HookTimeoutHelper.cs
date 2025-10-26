using TUnit.Core;
using TUnit.Core.Hooks;
using TUnit.Core.Interfaces;

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
        // CENTRAL POINT: At execution time, check if we should use a custom hook executor
        // This happens AFTER OnTestRegistered, so CustomHookExecutor will be set if the user called SetHookExecutor
        var timeout = hook.Timeout;

        if (timeout == null)
        {
            // No timeout specified, execute with potential custom executor
            return async () => await ExecuteHookWithPotentialCustomExecutor(hook, context, cancellationToken);
        }

        return async () =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutMs = (int)timeout.Value.TotalMilliseconds;
            cts.CancelAfter(timeoutMs);

            try
            {
                await ExecuteHookWithPotentialCustomExecutor(hook, context, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Hook '{hook.Name}' exceeded timeout of {timeoutMs}ms");
            }
        };
    }

    /// <summary>
    /// Executes a hook, using a custom executor if one is set on the TestContext
    /// </summary>
    private static ValueTask ExecuteHookWithPotentialCustomExecutor<T>(StaticHookMethod<T> hook, T context, CancellationToken cancellationToken)
    {
        // Check if this is a TestContext with a custom hook executor
        if (context is TestContext testContext && testContext.CustomHookExecutor != null)
        {
            // BYPASS the hook's default executor and call the custom executor directly with the hook's body
            var customExecutor = testContext.CustomHookExecutor;

            // Determine which executor method to call based on hook type
            if (hook is BeforeTestHookMethod || hook is InstanceHookMethod)
            {
                return customExecutor.ExecuteBeforeTestHook(
                    hook.MethodInfo,
                    testContext,
                    () => hook.Body!.Invoke(context, cancellationToken)
                );
            }
            else if (hook is AfterTestHookMethod)
            {
                return customExecutor.ExecuteAfterTestHook(
                    hook.MethodInfo,
                    testContext,
                    () => hook.Body!.Invoke(context, cancellationToken)
                );
            }
        }

        // No custom executor, use the hook's default executor
        return hook.ExecuteAsync(context, cancellationToken);
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
                throw new TimeoutException($"Hook '{hookName}' exceeded timeout of {timeoutMs}ms");
            }
        };
    }

    /// <summary>
    /// Creates a timeout-aware action wrapper for a hook delegate that returns ValueTask
    /// This overload is used for instance hooks (InstanceHookMethod)
    /// Custom executor handling for instance hooks is done in HookCollectionService.CreateInstanceHookDelegateAsync
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
                throw new TimeoutException($"Hook '{hookName}' exceeded timeout of {timeoutMs}ms");
            }
        };
    }
}