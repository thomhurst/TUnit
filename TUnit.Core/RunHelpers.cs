using System.Diagnostics;
using System.Runtime.CompilerServices;
using TUnit.Core.Exceptions;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Core;

internal static class RunHelpers
{
    internal static async Task RunWithTimeoutAsync(Func<CancellationToken, Task> taskDelegate, TimeSpan? timeout, CancellationToken token)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        var cancellationToken = cancellationTokenSource.Token;

        var taskCompletionSource = new TaskCompletionSource();

        await using var cancellationTokenRegistration = cancellationToken.Register(() =>
        {
            if (token.IsCancellationRequested)
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
        
        if (timeout != null)
        {
            cancellationTokenSource.CancelAfter(timeout.Value);
        }
        
        _ = taskDelegate(cancellationToken).ContinueWith(async t =>
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

        await taskCompletionSource.Task;
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