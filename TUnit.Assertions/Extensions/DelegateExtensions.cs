using System.Diagnostics;

namespace TUnit.Assertions.Extensions;

[StackTraceHidden]
internal static class DelegateExtensions
{
    public static async ValueTask<AssertionData> AsAssertionData(this Action action, string? actualExpression)
    {
        var start = DateTimeOffset.Now;

        try
        {
            await Task.Run(action);
            
            var end = DateTimeOffset.Now;

            return (null, null, actualExpression, start, end);
        }
        catch (Exception e)
        {
            var end = DateTimeOffset.Now;

            return (null, e, actualExpression, start, end);
        }
    }
    
    public static async ValueTask<AssertionData> AsAssertionData(this Func<ValueTask> action, string? actualExpression)
    {
        var start = DateTimeOffset.Now;

        try
        {
            await action();
            
            var end = DateTimeOffset.Now;

            return (null, null, actualExpression, start, end);
        }
        catch (Exception e)
        {
            var end = DateTimeOffset.Now;

            return (null, e, actualExpression, start, end);
        }
    }
    
    public static async ValueTask<AssertionData> AsAssertionData<T>(this Func<Task<T>> action, string? actualExpression)
    {
        var start = DateTimeOffset.Now;

        try
        {
            var result = await action();
            
            var end = DateTimeOffset.Now;
            
            return (result, null, actualExpression, start, end);
        }
        catch (Exception e)
        {
            var end = DateTimeOffset.Now;

            return (null, e, actualExpression, start, end);
        }
    }
    
    public static async ValueTask<AssertionData> AsAssertionData<T>(this Func<T> action, string? actualExpression)
    {
        var start = DateTimeOffset.Now;

        try
        {
            var result = await Task.Run(action);
            
            var end = DateTimeOffset.Now;

            return (result, null, actualExpression, start, end);
        }
        catch (Exception e)
        {
            var end = DateTimeOffset.Now;

            return (null, e, actualExpression, start, end);
        }
    }
    
    public static ValueTask<AssertionData> AsAssertionData<T>(this T t, string? actualExpression)
    {
        var start = DateTimeOffset.Now;

        try
        {
            return new ValueTask<AssertionData>((t, null, actualExpression, start, start));
        }
        catch (Exception e)
        {
            return new ValueTask<AssertionData>((null, e, actualExpression, start, start));
        }
    }
}