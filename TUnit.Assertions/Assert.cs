using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Helpers;
using TUnit.Assertions.Wrappers;

namespace TUnit.Assertions;

[UnconditionalSuppressMessage("Usage", "TUnitAssertions0002:Assert statements must be awaited")]
public static class Assert
{
    // All assertions return the unified AssertionBuilder<T>
    public static AssertionBuilder<TActual> That<TActual>(TActual value,
        [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }

    public static AssertionBuilder<IEnumerable<object>> That(IEnumerable enumerable,
        [CallerArgumentExpression(nameof(enumerable))] string? doNotPopulateThisValue = null)
    {
        return new AssertionBuilder<IEnumerable<object>>(
            new UnTypedEnumerableWrapper(enumerable), doNotPopulateThisValue);
    }

    public static AssertionBuilder<object?> That(Action value,
        [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AssertionBuilder<object?>(object? () =>
        {
            value();
            return null;
        }, doNotPopulateThisValue ?? "");
    }

    // This overload returns AssertionBuilder<T> for normal value assertions
    // If you need to test that the function throws, cast to Func<object?> first
    public static AssertionBuilder<TActual> That<TActual>(Func<TActual> value,
        [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }

    // Special overload for testing if a delegate throws
    public static AssertionBuilder<object?> ThatAction<TActual>(Func<TActual> value,
        [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AssertionBuilder<object?>(() => value(), doNotPopulateThisValue ?? "");
    }

    public static AssertionBuilder<object?> That(Func<Task> value,
        [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AssertionBuilder<object?>(object? () =>
        {
            value().GetAwaiter().GetResult();
            return null;
        }, doNotPopulateThisValue ?? "");
    }

    public static AssertionBuilder<object?> That(Task value,
        [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AssertionBuilder<object?>(object? () => { value.GetAwaiter().GetResult(); return null; }, doNotPopulateThisValue ?? "");
    }

    public static AssertionBuilder<TActual> That<TActual>(Func<Task<TActual>> value,
        [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }

    public static AssertionBuilder<TActual> That<TActual>(Task<TActual> value,
        [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }

    public static AssertionBuilder<object?> That(ValueTask value,
        [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AssertionBuilder<object?>(object? () => { value.AsTask().GetAwaiter().GetResult(); return null; }, doNotPopulateThisValue ?? "");
    }

    public static AssertionBuilder<TActual> That<TActual>(ValueTask<TActual> value,
        [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }

    public static IDisposable Multiple()
    {
        return new AssertionScope();
    }

    public static ExceptionAssertionBuilder<Exception> ThrowsAsync(Type exceptionType, Func<Task> @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
    {
        // Create an ExceptionAssertionBuilder with a delegate that validates the exception type
        return new ExceptionAssertionBuilder<Exception>(
            async () => {
                await @delegate();
                throw new AssertionException($"Expected exception of type {exceptionType.Name} but no exception was thrown");
            }, 
            doNotPopulateThisValue).WithExceptionTypeValidation(exceptionType);
    }
    
    public static ExceptionAssertionBuilder<Exception> ThrowsAsync(Func<Task> @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
    {
        return new ExceptionAssertionBuilder<Exception>(@delegate, doNotPopulateThisValue);
    }

    public static ExceptionAssertionBuilder<TException> ThrowsAsync<TException>(Func<Task> @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        where TException : Exception
    {
        return new ExceptionAssertionBuilder<TException>(@delegate, doNotPopulateThisValue);
    }

    // Overload for Task directly
    public static ExceptionAssertionBuilder<Exception> ThrowsAsync(Task task,
        [CallerArgumentExpression(nameof(task))] string? doNotPopulateThisValue = null)
    {
        return new ExceptionAssertionBuilder<Exception>(() => task, doNotPopulateThisValue);
    }

    // Overload for ValueTask directly
    public static ExceptionAssertionBuilder<Exception> ThrowsAsync(ValueTask valueTask,
        [CallerArgumentExpression(nameof(valueTask))] string? doNotPopulateThisValue = null)
    {
        return new ExceptionAssertionBuilder<Exception>(() => valueTask.AsTask(), doNotPopulateThisValue);
    }

    public static ExceptionAssertionBuilder<Exception> Throws(Type exceptionType, Action @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
    {
        // Create an ExceptionAssertionBuilder with a delegate that validates the exception type  
        return new ExceptionAssertionBuilder<Exception>(
            () => {
                @delegate();
                throw new AssertionException($"Expected exception of type {exceptionType.Name} but no exception was thrown");
            }, 
            doNotPopulateThisValue).WithExceptionTypeValidation(exceptionType);
    }
    
    public static ExceptionAssertionBuilder<Exception> Throws(Action @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
    {
        return new ExceptionAssertionBuilder<Exception>(@delegate, doNotPopulateThisValue);
    }

    public static ExceptionAssertionBuilder<TException> Throws<TException>(Action @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        where TException : Exception
    {
        return new ExceptionAssertionBuilder<TException>(@delegate, doNotPopulateThisValue);
    }

    public static void Fail(string reason)
    {
        try
        {
            TUnit.Assertions.Fail.Test(reason);
        }
        catch (AssertionException e) when (AssertionScope.GetCurrentAssertionScope() is { } assertionScope)
        {
            assertionScope.AddException(e);
        }
    }
}
