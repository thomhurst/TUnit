using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

internal static class Warn
{
    public static ValueAssertionBuilder<TActual> Unless<TActual>(TActual value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "") 
    {
        return new ValueAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static DelegateAssertionBuilder Unless(Action value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertionBuilder(value, doNotPopulateThisValue);
    }
    
    public static ValueDelegateAssertionBuilder<TActual> Unless<TActual>(Func<TActual> value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new ValueDelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder Unless(Func<Task> value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new AsyncDelegateAssertionBuilder(value, doNotPopulateThisValue);
    }
    
    public static AsyncValueDelegateAssertionBuilder<TActual> Unless<TActual>(Func<Task<TActual>> value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }

    public static IDisposable Multiple()
    {
        return new AssertionScope();
    }

    [DoesNotReturn]
    public static void Fail(string reason)
    {
        throw new AssertionException(reason);
    }

    public static Task<Exception> ThrowsAsync(Func<Task> @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync<Exception>(@delegate, doNotPopulateThisValue);
    
    public static async Task<TException> ThrowsAsync<TException>(Func<Task> @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
    {
        try
        {
            await @delegate();
            Fail($"No exception was thrown by {doNotPopulateThisValue.GetStringOr("the delegate")}");
        }
        catch (Exception e) when(e is not AssertionException)
        {
            if (e is TException exception)
            {
                return exception;
            }
            
            Fail($"Exception is of type {e.GetType().Name} instead of {typeof(TException).Name} for {doNotPopulateThisValue.GetStringOr("the delegate")}");
        }

        return null!;
    }

    public static Exception ThrowsAsync(Action @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync<Exception>(@delegate, doNotPopulateThisValue);
    
    public static TException ThrowsAsync<TException>(Action @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
    {
        try
        {
            @delegate();
            Fail($"No exception was thrown by {doNotPopulateThisValue.GetStringOr("the delegate")}");
        }
        catch (Exception e) when(e is not AssertionException)
        {
            if (e is TException exception)
            {
                return exception;
            }
            
            Fail($"Exception is of type {e.GetType().Name} instead of {typeof(TException).Name} for {doNotPopulateThisValue.GetStringOr("the delegate")}");
        }

        return null!;
    }
}