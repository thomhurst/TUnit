using System.Diagnostics;
using System.Reflection;
using TUnit.Core.Exceptions;
using TUnit.Engine.Extensions;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Engine;

public static class RunHelpers
{
    public static Task RunAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }
    
    public static async Task RunAsync(Func<Task> action)
    {
        await action();
    }
    
    public static async Task RunAsync(Func<ValueTask> action)
    {
        await action();
    }
    
    public static Task RunWithTimeoutAsync(Action action, CancellationToken cancellationToken)
    {
        return RunWithTimeoutAsync(RunAsync(action), cancellationToken);
    }
    
    public static Task RunWithTimeoutAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        return RunWithTimeoutAsync(RunAsync(action), cancellationToken);
    }
    
    public static Task RunWithTimeoutAsync(Func<ValueTask> action, CancellationToken cancellationToken)
    {
        return RunWithTimeoutAsync(RunAsync(action), cancellationToken);
    }

    private static async Task RunWithTimeoutAsync(Task task, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var taskCompletionSource = new TaskCompletionSource();

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

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                if (EngineCancellationToken.Token.IsCancellationRequested)
                {
                    taskCompletionSource.TrySetException(new TestRunCanceledException());
                    return;
                }
                
                taskCompletionSource.TrySetException(new TimeoutException(stopwatch.Elapsed));
            });
        }

        await taskCompletionSource.Task;
    }
    
    public static Task RunWithTimeoutAsync(Action action, TimeSpan? timeout)
    {
        return RunWithTimeoutAsync(RunAsync(action), timeout);
    }
    
    public static Task RunWithTimeoutAsync(Func<Task> action, TimeSpan? timeout)
    {
        return RunWithTimeoutAsync(RunAsync(action), timeout);
    }
    
    public static Task RunWithTimeoutAsync(Func<ValueTask> action, TimeSpan? timeout)
    {
        return RunWithTimeoutAsync(RunAsync(action), timeout);
    }

    private static async Task RunWithTimeoutAsync(Task task, TimeSpan? timeout)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(EngineCancellationToken.Token);
        
        if (timeout != null)
        {
            cancellationTokenSource.CancelAfter(timeout.Value);
        }

        await RunWithTimeoutAsync(task, cancellationTokenSource.Token);
    }
    
    public static Task RunWithTimeoutAsync(Action action, MethodInfo methodInfo)
    {
        return RunWithTimeoutAsync(RunAsync(action), methodInfo);
    }
    
    public static Task RunWithTimeoutAsync(Func<Task> action, MethodInfo methodInfo)
    {
        return RunWithTimeoutAsync(RunAsync(action), methodInfo);
    }
    
    public static Task RunWithTimeoutAsync(Func<ValueTask> action, MethodInfo methodInfo)
    {
        return RunWithTimeoutAsync(RunAsync(action), methodInfo);
    }

    private static async Task RunWithTimeoutAsync(Task task, MethodInfo methodInfo)
    {
        await RunWithTimeoutAsync(task, methodInfo.GetTimeout());
    }

    public static async Task RunSafelyAsync(Action action, List<Exception> exceptions)
    {
        try
        {
            action();
            await Task.CompletedTask;
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }
    }
    
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
    
    public static Task RunSafelyWithTimeoutAsync(Action action, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        return RunSafelyWithTimeoutAsync(RunAsync(action), exceptions, cancellationToken);
    }
    
    public static Task RunSafelyWithTimeoutAsync(Func<Task> action, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        return RunSafelyWithTimeoutAsync(RunAsync(action), exceptions, cancellationToken);
    }
    
    public static Task RunSafelyWithTimeoutAsync(Func<ValueTask> action, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        return RunSafelyWithTimeoutAsync(RunAsync(action), exceptions, cancellationToken);
    }

    private static async Task RunSafelyWithTimeoutAsync(Task task, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        try
        {
            await RunWithTimeoutAsync(task, cancellationToken);
        }
        catch (Exception e)
        {
            exceptions.Add(e);
        }
    }
    
    public static ValueTask Dispose(object? obj)
    {
        if (obj is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        if (obj is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}