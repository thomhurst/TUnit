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
    
    public static async Task ExecuteWithRetries(TestInformation testInformation, Func<Task> testDelegate)
    {
        var retryCount = testInformation.RetryCount;
        
        // +1 for the original non-retry
        for (var i = 0; i < retryCount + 1; i++)
        {
            try
            {
                await testDelegate();
                break;
            }
            catch (Exception e)
            {
                if (i == retryCount || !await ShouldRetry(testInformation, e))
                {
                    throw;
                }
            }
        }
    }

    private static async Task<bool> ShouldRetry(TestInformation testInformation, Exception e)
    {
        try
        {
            var retryAttribute = testInformation.LazyRetryAttribute.Value;

            if (retryAttribute == null)
            {
                return false;
            }

            return await retryAttribute.ShouldRetry(testInformation, e);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return false;
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