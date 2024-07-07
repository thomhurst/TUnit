using System.Diagnostics;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Core;

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
    
    public static async Task RunWithTimeoutAsync(Task task, CancellationToken cancellationToken)
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
            cancellationToken.Register(() => taskCompletionSource.TrySetException(new TimeoutException(stopwatch.Elapsed)));
        }

        await taskCompletionSource.Task;
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