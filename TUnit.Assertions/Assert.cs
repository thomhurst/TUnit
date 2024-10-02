using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public static class Assert
{
    public static ValueAssertionBuilder<TActual> That<TActual>(TActual value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new ValueAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static DelegateAssertionBuilder That(Action value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertionBuilder(value, doNotPopulateThisValue);
    }
    
    public static ValueDelegateAssertionBuilder<TActual> That<TActual>(Func<TActual> value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new ValueDelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder That(Func<Task> value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncDelegateAssertionBuilder(value, doNotPopulateThisValue);
    }
    
    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(Func<Task<TActual>> value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder That(Task value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncDelegateAssertionBuilder(async () => await value, doNotPopulateThisValue);
    }
    
    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(Task<TActual> value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(async () => await value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder That(ValueTask value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncDelegateAssertionBuilder(async () => await value, doNotPopulateThisValue);
    }
    
    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(ValueTask<TActual> value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(async () => await value, doNotPopulateThisValue);
    }

    public static IAsyncDisposable Multiple()
    {
        return new AssertionScope();
    }

    [DoesNotReturn]
    public static void Fail(string reason)
    {
        throw new AssertionException(reason);
    }

    public static Task<Exception> ThrowsAsync(Func<Task> @delegate,
        [CallerArgumentExpression("delegate")] string? doNotPopulateThisValue = null)
        => ThrowsAsync<Exception>(@delegate, doNotPopulateThisValue);

    public static Task<Exception> ThrowsAsync(Task @delegate,
        [CallerArgumentExpression("delegate")] string? doNotPopulateThisValue = null)
        => ThrowsAsync(async () => await @delegate, doNotPopulateThisValue);
    
    public static Task<Exception> ThrowsAsync(ValueTask @delegate,
        [CallerArgumentExpression("delegate")] string? doNotPopulateThisValue = null)
        => ThrowsAsync(async () => await @delegate, doNotPopulateThisValue);
    
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

    public static Task<TException> ThrowsAsync<TException>(Task @delegate,
        [CallerArgumentExpression("delegate")] string? doNotPopulateThisValue = null) where TException : Exception
        => ThrowsAsync<TException>(async () => await @delegate, doNotPopulateThisValue);
    
    public static Task<TException> ThrowsAsync<TException>(ValueTask @delegate,
        [CallerArgumentExpression("delegate")] string? doNotPopulateThisValue = null) where TException : Exception
        => ThrowsAsync<TException>(async () => await @delegate, doNotPopulateThisValue);

    public static Exception Throws(Action @delegate,
        [CallerArgumentExpression("delegate")] string? doNotPopulateThisValue = null)
        => Throws<Exception>(@delegate, doNotPopulateThisValue);
    
    public static TException Throws<TException>(Action @delegate, [CallerArgumentExpression("delegate")] string? doNotPopulateThisValue = null) where TException : Exception
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