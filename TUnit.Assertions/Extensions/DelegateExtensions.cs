using System.Diagnostics;

namespace TUnit.Assertions.Extensions;

[StackTraceHidden]
internal static class DelegateExtensions
{
    public static ValueTask<AssertionData<object?>> AsAssertionData(this Action action, string? actualExpression)
    {
        try
        {
            action();
            return new ValueTask<AssertionData<object?>>((null, null, actualExpression));
        }
        catch (Exception e)
        {
            return new ValueTask<AssertionData<object?>>((null, e, actualExpression));
        }
    }
    
    public static async ValueTask<AssertionData<object?>> AsAssertionData(this Func<Task> action, string? actualExpression)
    {
        try
        {
            await action();
            return (null, null, actualExpression);
        }
        catch (Exception e)
        {
            return (null, e, actualExpression);
        }
    }
    
    public static async ValueTask<AssertionData<T>> AsAssertionData<T>(this Func<Task<T>> action, string? actualExpression)
    {
        try
        {
            return (await action(), null, actualExpression);
        }
        catch (Exception e)
        {
            return (default, e, actualExpression);
        }
    }
    
    public static ValueTask<AssertionData<T>> AsAssertionData<T>(this Func<T> action, string? actualExpression)
    {
        try
        {
            return new ValueTask<AssertionData<T>>((action(), null, actualExpression));
        }
        catch (Exception e)
        {
            return new ValueTask<AssertionData<T>>((default, e, actualExpression));
        }
    }
    
    public static ValueTask<AssertionData<T>> AsAssertionData<T>(this T t, string? actualExpression)
    {
        try
        {
            return new ValueTask<AssertionData<T>>((t, null, actualExpression));
        }
        catch (Exception e)
        {
            return new ValueTask<AssertionData<T>>((default, e, actualExpression));
        }
    }
}