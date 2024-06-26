namespace TUnit.Core.Helpers;

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