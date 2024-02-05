using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions;

public static class Assert
{
    public static ValueAssertionBuilder<TActual> That<TActual>(TActual value)
    {
        return new ValueAssertionBuilder<TActual>(value);
    }
    
    public static DelegateAssertionBuilder That(Action value)
    {
        return new DelegateAssertionBuilder(value);
    }
    
    public static DelegateAssertionBuilder<TActual> That<TActual>(Func<TActual> value)
    {
        return new DelegateAssertionBuilder<TActual>(value);
    }
    
    public static AsyncDelegateAssertionBuilder That(Func<Task> value)
    {
        return new AsyncDelegateAssertionBuilder(value);
    }
    
    public static AsyncDelegateAssertionBuilder<TActual> That<TActual>(Func<Task<TActual>> value)
    {
        return new AsyncDelegateAssertionBuilder<TActual>(value!);
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
    
    public static async Task<TException> ThrowsAsync<TException>(Func<Task> @delegate) where TException : Exception
    {
        try
        {
            await @delegate();
            Fail("No exception was thrown");
        }
        catch (Exception e)
        {
            if (e is TException exception)
            {
                return exception;
            }
            
            Fail($"Exception is of type {e.GetType().Name} instead of {typeof(TException).Name}");
        }

        return null!;
    }
    
    public static TException ThrowsAsync<TException>(Action @delegate) where TException : Exception
    {
        try
        {
            @delegate();
            Fail("No exception was thrown");
        }
        catch (Exception e)
        {
            if (e is TException exception)
            {
                return exception;
            }
            
            Fail($"Exception is of type {e.GetType().Name} instead of {typeof(TException).Name}");
        }

        return null!;
    }
}