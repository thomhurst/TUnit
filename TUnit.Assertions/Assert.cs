using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions;

public static class Assert
{
    public static ValueAssertionBuilder<TActual> That<TActual>(TActual value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new ValueAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static DelegateAssertionBuilder That(Action value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertionBuilder(value, doNotPopulateThisValue);
    }
    
    public static ValueDelegateAssertionBuilder<TActual> That<TActual>(Func<TActual> value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new ValueDelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder That(Func<Task> value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new AsyncDelegateAssertionBuilder(value, doNotPopulateThisValue);
    }
    
    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(Func<Task<TActual>> value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder That(Task value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new AsyncDelegateAssertionBuilder(async () => await value, doNotPopulateThisValue);
    }
    
    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(Task<TActual> value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(async () => await value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder That(ValueTask value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new AsyncDelegateAssertionBuilder(async () => await value, doNotPopulateThisValue);
    }
    
    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(ValueTask<TActual> value, [CallerArgumentExpression(nameof(value))] string doNotPopulateThisValue = "")
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(async () => await value, doNotPopulateThisValue);
    }

    public static IDisposable Multiple()
    {
        return new AssertionScope();
    }

    public static Task<Exception> ThrowsAsync(Func<Task> @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync<Exception>(@delegate, doNotPopulateThisValue);

    public static Task<Exception> ThrowsAsync(Task @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync(async () => await @delegate, doNotPopulateThisValue);
    
    public static Task<Exception> ThrowsAsync(ValueTask @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync(async () => await @delegate, doNotPopulateThisValue);
    
    public static async Task<TException> ThrowsAsync<TException>(Func<Task> @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
    {
        try
        {
            await @delegate();
        }
        catch (TException e)
        {
            return e;
        }
        catch (Exception e)
        {
            Fail($"Exception is of type {e.GetType().Name} instead of {typeof(TException).Name} for {doNotPopulateThisValue.GetStringOr("the delegate")}");
        }

        Fail($"No exception was thrown by {doNotPopulateThisValue.GetStringOr("the delegate")}");
        
        return null!;
    }

    public static Task<TException> ThrowsAsync<TException>(Task @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
        => ThrowsAsync<TException>(async () => await @delegate, doNotPopulateThisValue);
    
    public static Task<TException> ThrowsAsync<TException>(ValueTask @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
        => ThrowsAsync<TException>(async () => await @delegate, doNotPopulateThisValue);

    public static Exception Throws(Action @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => Throws<Exception>(@delegate, doNotPopulateThisValue);
    
    public static TException Throws<TException>(Action @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
    {
        try
        {
            @delegate();
        }
        catch (TException e)
        {
            return e;
        }
        catch (Exception e)
        {
            Fail($"Exception is of type {e.GetType().Name} instead of {typeof(TException).Name} for {doNotPopulateThisValue.GetStringOr("the delegate")}");
        }
        
        Fail($"No exception was thrown by {doNotPopulateThisValue.GetStringOr("the delegate")}");


        return null!;
    }

    public static void Fail(string reason) => TUnit.Assertions.Fail.Test(reason);
}