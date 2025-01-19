using System.Diagnostics;

namespace TUnit.Assertions.Extensions;

[StackTraceHidden]
internal static class DelegateExtensions
{
    public static Func<ValueTask<AssertionData<object?>>> AsAssertionData(this Action action, string? actualExpression)
    {
        return () =>
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
        };
    }
    
    public static Func<ValueTask<AssertionData<object?>>> AsAssertionData(this Func<Task> action, string? actualExpression)
    {
        return async () =>
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
        };
    }
    
    public static Func<ValueTask<AssertionData<T>>> AsAssertionData<T>(this Func<Task<T>> action, string? actualExpression)
    {
        return async () =>
        {
            try
            {
                return (await action(), null, actualExpression);
            }
            catch (Exception e)
            {
                return (default, e, actualExpression);
            }
        };
    }
    
    public static Func<ValueTask<AssertionData<T>>> AsAssertionData<T>(this Func<T> action, string? actualExpression)
    {
        return () =>
        {
            try
            {
                return new ValueTask<AssertionData<T>>((action(), null, actualExpression));
            }
            catch (Exception e)
            {
                return new ValueTask<AssertionData<T>>((default, e, actualExpression));
            }
        };
    }
    
    public static Func<ValueTask<AssertionData<T>>> AsAssertionData<T>(this T t, string? actualExpression)
    {
        try
        {
            return () => new ValueTask<AssertionData<T>>((t, null, actualExpression));
        }
        catch (Exception e)
        {
            return () => new ValueTask<AssertionData<T>>((default, e, actualExpression));
        }
    }
}