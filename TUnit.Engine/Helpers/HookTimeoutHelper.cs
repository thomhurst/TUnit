using System.Diagnostics.CodeAnalysis;
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
    public static Task CreateTimeoutHookAction<T>(
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
            return ExecuteHookWithPotentialCustomExecutor(hook, context, cancellationToken).AsTask();
        }

        var timeoutMs = (int)timeout.Value.TotalMilliseconds;

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
                await ExecuteHookWithPotentialCustomExecutor(hook, context, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                var baseMessage = $"Hook '{hook.Name}' exceeded timeout of {timeoutMs}ms";
                throw new TimeoutException(TimeoutDiagnostics.BuildTimeoutDiagnosticsMessage(baseMessage, executionTask: null));
            }
        }
    }

    /// <summary>
    /// Executes a hook, using a custom executor if one is set on the TestContext.
    /// Precedence: the hook's own HookExecutor wins if it was set explicitly. Only fall back
    /// to the test-level CustomHookExecutor when the hook is still on DefaultExecutor —
    /// this preserves the #2666 scenario (SetHookExecutor fills in for hooks that don't
    /// specify their own executor) without overriding hooks that explicitly did.
    /// </summary>
    private static ValueTask ExecuteHookWithPotentialCustomExecutor<T>(StaticHookMethod<T> hook, T context, CancellationToken cancellationToken)
    {
        // Only consider CustomHookExecutor when the hook itself is still on the default
        // executor — an explicit [HookExecutor<T>] attribute on the hook must win.
        if (context is TestContext testContext
            && testContext.CustomHookExecutor != null
            && ReferenceEquals(hook.HookExecutor, DefaultExecutor.Instance))
        {
            var customExecutor = testContext.CustomHookExecutor;

            // Determine which executor method to call based on hook type
            if (hook is BeforeTestHookMethod || hook is InstanceHookMethod)
            {
                return ExecuteBeforeTestHook(hook, context, cancellationToken, customExecutor, testContext);
            }
            else if (hook is AfterTestHookMethod)
            {
                return ExecuteAfterTestHook(hook, context, cancellationToken, customExecutor, testContext);
            }
        }

        // Use the hook's own executor (default or explicit)
        return hook.ExecuteAsync(context, cancellationToken);
    }

    private static ValueTask ExecuteBeforeTestHook<T>(StaticHookMethod<T> hook, [DisallowNull] T context,
        CancellationToken cancellationToken, IHookExecutor customExecutor, TestContext testContext) =>
        customExecutor.ExecuteBeforeTestHook(
            hook.MethodInfo,
            testContext,
            () => hook.Body!.Invoke(context, cancellationToken)
        );

    private static ValueTask ExecuteAfterTestHook<T>(StaticHookMethod<T> hook, [DisallowNull] T context,
        CancellationToken cancellationToken, IHookExecutor customExecutor, TestContext testContext) =>
        customExecutor.ExecuteAfterTestHook(
            hook.MethodInfo,
            testContext,
            () => hook.Body!.Invoke(context, cancellationToken)
        );

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

        var timeoutMs = (int)timeout.Value.TotalMilliseconds;

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
    /// Creates a timeout-aware action wrapper for a hook delegate that returns ValueTask
    /// This overload is used for instance hooks (InstanceHookMethod)
    /// Custom executor handling for instance hooks is done in HookDelegateBuilder.CreateInstanceHookDelegateAsync
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

        var timeoutMs = (int)timeout.Value.TotalMilliseconds;

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
