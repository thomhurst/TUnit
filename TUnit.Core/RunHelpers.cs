using System.Diagnostics;
using System.Runtime.CompilerServices;
using TUnit.Core.Exceptions;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Core;

internal static class RunHelpers
{
    internal static async Task RunWithTimeoutAsync(Func<CancellationToken, Task> taskDelegate, TimeSpan? timeout, EngineCancellationToken engineCancellationToken)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(engineCancellationToken.Token);

        var cancellationToken = cancellationTokenSource.Token;

        var taskCompletionSource = new TaskCompletionSource();

        var task = taskDelegate(cancellationToken);

        _ = task.ContinueWith(async t =>
        {
            try
            {
                await t;
                taskCompletionSource.TrySetResult();
            }
            catch (Exception e)
            {
                taskCompletionSource.TrySetException(e);
            }
        }, CancellationToken.None);

        CancellationTokenRegistration? cancellationTokenRegistration = null;
        if (cancellationToken.CanBeCanceled)
        {
            cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                if (engineCancellationToken.Token.IsCancellationRequested)
                {
                    taskCompletionSource.TrySetException(new TestRunCanceledException());
                    return;
                }

                if (timeout.HasValue)
                {
                    taskCompletionSource.TrySetException(new TimeoutException(timeout.Value));
                }
                else
                {
                    taskCompletionSource.TrySetCanceled(cancellationToken);
                }
            });
        }

        if (timeout != null)
        {
            cancellationTokenSource.CancelAfter(timeout.Value);
        }

        await using (cancellationTokenRegistration)
        {
            await taskCompletionSource.Task;
        }
    }
    
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static async Task RunSafelyAsync(Func<Task> action, List<Exception> exceptions)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }
    }
    
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static async Task RunValueTaskSafelyAsync(Func<ValueTask> action, List<Exception> exceptions)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }
    }
}