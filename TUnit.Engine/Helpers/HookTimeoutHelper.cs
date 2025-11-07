using System.Threading.Tasks;
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
        var timeout = hook.Timeout;

        if (timeout == null)
        {
            return async () => await ExecuteHookWithPotentialCustomExecutor(hook, context, cancellationToken);
        }

        var timeProvider = (context as TestContext)?.TimeProvider ?? TimeProvider.System;

        return async () =>
        {
            await ExecuteWithTimeoutAsync(
                async ct => await ExecuteHookWithPotentialCustomExecutor(hook, context, ct),
                timeout.Value,
                cancellationToken,
                $"Hook '{hook.Name}' exceeded timeout of {(int)timeout.Value.TotalMilliseconds}ms",
                timeProvider);
        };
    }

    /// <summary>
    /// Executes a hook, using a custom executor if one is set on the TestContext
    /// </summary>
    private static ValueTask ExecuteHookWithPotentialCustomExecutor<T>(StaticHookMethod<T> hook, T context, CancellationToken cancellationToken)
    {
        if (context is TestContext testContext && testContext.CustomHookExecutor != null)
        {
            var customExecutor = testContext.CustomHookExecutor;

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
            return async () => await hookDelegate(context, cancellationToken);
        }

        var timeProvider = (context as TestContext)?.TimeProvider ?? TimeProvider.System;

        return async () =>
        {
            await ExecuteWithTimeoutAsync(
                async ct => await hookDelegate(context, ct),
                timeout.Value,
                cancellationToken,
                $"Hook '{hookName}' exceeded timeout of {(int)timeout.Value.TotalMilliseconds}ms",
                timeProvider);
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
            return async () => await hookDelegate(context, cancellationToken);
        }

        var timeProvider = (context as TestContext)?.TimeProvider ?? TimeProvider.System;

        return async () =>
        {
            await ExecuteWithTimeoutAsync(
                async ct => await hookDelegate(context, ct),
                timeout.Value,
                cancellationToken,
                $"Hook '{hookName}' exceeded timeout of {(int)timeout.Value.TotalMilliseconds}ms",
                timeProvider);
        };
    }

    /// <summary>
    /// Executes a task with timeout using TimeProvider for testability
    /// </summary>
    private static async Task ExecuteWithTimeoutAsync(
        Func<CancellationToken, Task> taskFactory,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        string timeoutMessage,
        TimeProvider timeProvider)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        var executionTask = taskFactory(timeoutCts.Token);

        // Use a cancellable timeout task to avoid leaving Task.Delay running in the background
        using var timeoutTaskCts = new CancellationTokenSource();
        var timeoutTask = timeProvider.Delay(timeout, timeoutTaskCts.Token);

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

            throw new TimeoutException(timeoutMessage);
        }

        // Task completed normally - cancel the timeout task to free resources immediately
        timeoutTaskCts.Cancel();

        // Await the result to propagate any exceptions
        await executionTask.ConfigureAwait(false);
    }
}