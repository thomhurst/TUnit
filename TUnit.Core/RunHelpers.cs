using System.Diagnostics;
using System.Runtime.CompilerServices;
using TUnit.Core.Exceptions;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Core;

internal static class RunHelpers
{
    internal static async Task RunWithTimeoutAsync(Func<CancellationToken, Task> taskDelegate, TimeSpan timeout, CancellationToken token)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        var cancellationToken = cancellationTokenSource.Token;

        var taskCompletionSource = new TaskCompletionSource<bool>();

        using var cancellationTokenRegistration = cancellationToken.Register(() =>
        {
            if (token.IsCancellationRequested)
            {
                taskCompletionSource.TrySetException(new TestRunCanceledException());
                return;
            }

            taskCompletionSource.TrySetException(new TimeoutException(timeout));
        });
        
        cancellationTokenSource.CancelAfter(timeout);

        try
        {
            await await Task.WhenAny
            (
                taskDelegate(cancellationToken),
                taskCompletionSource.Task
            );
        }
        finally
        {
            // Try set result if it doesn't have one so it finishes
            // and doesn't stay pending in background
            taskCompletionSource.TrySetResult(false);
        }
    }
    
    [StackTraceHidden]
#if NET
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void RunSafely(Action action, List<Exception> exceptions)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }
    }
    
    [StackTraceHidden]
#if NET
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static async Task RunSafelyAsync(Func<ValueTask> action, List<Exception> exceptions)
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
#if NET
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
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