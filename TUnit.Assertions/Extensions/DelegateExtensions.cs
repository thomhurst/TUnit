namespace TUnit.Assertions.Extensions;

internal static class DelegateExtensions
{
    public static Exception? InvokeAndGetException(this Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }
    
    public static async Task<Exception?> InvokeAndGetExceptionAsync(this Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }
    
    public static async Task<(T?, Exception?)> InvokeAndGetExceptionAsync<T>(this Func<Task<T>> action)
    {
        try
        {
            return (await action(), null);
        }
        catch (Exception e)
        {
            return (default, e);
        }
    }
    
    public static (T?, Exception?) InvokeAndGetException<T>(this Func<T> action)
    {
        try
        {
            return (action(), null);
        }
        catch (Exception e)
        {
            return (default, e);
        }
    }
}