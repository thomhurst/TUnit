namespace TUnit.Assertions.Extensions;

internal static class DelegateExtensions
{
    public static Task<AssertionData<object?>> AsAssertionData(this Action action)
    {
        try
        {
            action();
            return Task.FromResult<AssertionData<object?>>((null, null));
        }
        catch (Exception e)
        {
            return Task.FromResult<AssertionData<object?>>((null, e));
        }
    }
    
    public static async Task<AssertionData<object?>> AsAssertionData(this Func<Task> action)
    {
        try
        {
            await action();
            return (null, null);
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }
    
    public static async Task<AssertionData<T>> AsAssertionData<T>(this Func<Task<T>> action)
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
    
    public static Task<AssertionData<T>> AsAssertionData<T>(this Func<T> action)
    {
        try
        {
            return Task.FromResult<AssertionData<T>>((action(), null));
        }
        catch (Exception e)
        {
            return Task.FromResult<AssertionData<T>>((default, e));
        }
    }
    
    public static Func<Task<AssertionData<T>>> AsAssertionData<T>(this T t)
    {
        try
        {
            return () => Task.FromResult<AssertionData<T>>((t, null));
        }
        catch (Exception e)
        {
            return () => Task.FromResult<AssertionData<T>>((default, e));
        }
    }
}