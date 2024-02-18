using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions;

public static class Assert
{
    public static ValueAssertionBuilder<TActual> That<TActual>(TActual value, [CallerArgumentExpression("value")] string? doNotPopulateThisValue = null)
    {
        return new ValueAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static DelegateAssertionBuilder That(Action value, [CallerArgumentExpression("value")] string? doNotPopulateThisValue = null)
    {
        return new DelegateAssertionBuilder(value, doNotPopulateThisValue);
    }
    
    public static DelegateAssertionBuilder<TActual> That<TActual>(Func<TActual> value, [CallerArgumentExpression("value")] string? doNotPopulateThisValue = null)
    {
        return new DelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder That(Func<Task> value, [CallerArgumentExpression("value")] string? doNotPopulateThisValue = null)
    {
        return new AsyncDelegateAssertionBuilder(value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder<TActual> That<TActual>(Func<Task<TActual>> value, [CallerArgumentExpression("value")] string? doNotPopulateThisValue = null)
    {
        return new AsyncDelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }

    public static AssertMultipleHandler Multiple(Action action)
    {
        return new AssertMultipleHandler(action);
    }

    [DoesNotReturn]
    public static void Fail(string reason)
    {
        throw new AssertionException(reason);
    }

    public static Task<Exception> ThrowsAsync(Func<Task> @delegate,
        [CallerArgumentExpression("delegate")] string? doNotPopulateThisValue = null)
        => ThrowsAsync<Exception>(@delegate, doNotPopulateThisValue);
    
    public static async Task<TException> ThrowsAsync<TException>(Func<Task> @delegate, [CallerArgumentExpression("delegate")] string? doNotPopulateThisValue = null) where TException : Exception
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
        [CallerArgumentExpression("delegate")] string? doNotPopulateThisValue = null)
        => ThrowsAsync<Exception>(@delegate, doNotPopulateThisValue);
    
    public static TException ThrowsAsync<TException>(Action @delegate, [CallerArgumentExpression("delegate")] string? doNotPopulateThisValue = null) where TException : Exception
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