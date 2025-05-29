using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;
using TUnit.Assertions.Wrappers;

namespace TUnit.Assertions;

public static class Assert
{
    public static ValueAssertionBuilder<TActual> That<TActual>(TActual value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new ValueAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }

    public static ValueAssertionBuilder<IEnumerable<object>> That(IEnumerable enumerable, [CallerArgumentExpression(nameof(enumerable))] string? doNotPopulateThisValue = null)
    {
        return new ValueAssertionBuilder<IEnumerable<object>>(new UnTypedEnumerableWrapper(enumerable), doNotPopulateThisValue);
    }

    public static DelegateAssertionBuilder That(Action value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new DelegateAssertionBuilder(value, doNotPopulateThisValue);
    }

    public static ValueDelegateAssertionBuilder<TActual> That<TActual>(Func<TActual> value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new ValueDelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }

    public static AsyncDelegateAssertionBuilder That(Func<Task> value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AsyncDelegateAssertionBuilder(value, doNotPopulateThisValue);
    }

    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(Func<Task<TActual>> value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }

    public static AsyncDelegateAssertionBuilder That(Task value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AsyncDelegateAssertionBuilder(async () => await value, doNotPopulateThisValue);
    }

    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(Task<TActual> value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(async () => await value, doNotPopulateThisValue);
    }

    public static AsyncDelegateAssertionBuilder That(ValueTask value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AsyncDelegateAssertionBuilder(async () => await value, doNotPopulateThisValue);
    }

    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(ValueTask<TActual> value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
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

    public static Task<TException> ThrowsAsync<TException>(Task @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
        => ThrowsAsync<TException>(async () => await @delegate, doNotPopulateThisValue);

    public static Task<TException> ThrowsAsync<TException>(ValueTask @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
        => ThrowsAsync<TException>(async () => await @delegate, doNotPopulateThisValue);

    public static async Task<TException> ThrowsAsync<TException>(Func<Task> @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
    {
        return (TException)await ThrowsAsync(typeof(TException), @delegate, doNotPopulateThisValue);
    }

    public static Task<TException> ThrowsAsync<TException>(string parameterName,
        Task @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : ArgumentException
        => ThrowsAsync<TException>(parameterName, async () => await @delegate, doNotPopulateThisValue);

    public static Task<TException> ThrowsAsync<TException>(string parameterName,
        ValueTask @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : ArgumentException
        => ThrowsAsync<TException>(parameterName, async () => await @delegate, doNotPopulateThisValue);

    public static async Task<TException> ThrowsAsync<TException>(string parameterName,
        Func<Task> @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : ArgumentException
    {
        var ex = (TException)await ThrowsAsync(typeof(TException), @delegate, doNotPopulateThisValue);

        if (ex.ParamName?.Equals(parameterName, StringComparison.Ordinal) == false)
        {
            Fail($"Incorrect parameter name {new StringDifference(ex.ParamName, parameterName).ToString("it differs at index")}");
        }

        return ex;
    }

    public static Task<Exception> ThrowsAsync(Type type, Task @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync(type, async () => await @delegate, doNotPopulateThisValue);

    public static Task<Exception> ThrowsAsync(Type type, ValueTask @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync(type, async () => await @delegate, doNotPopulateThisValue);

    public static async Task<Exception> ThrowsAsync(Type type, Func<Task> @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
    {
        try
        {
            await @delegate();
        }
        catch (Exception e) when (type.IsAssignableFrom(e.GetType()))
        {
            return e;
        }
        catch (Exception e)
        {
            Fail($"Exception is of type {e.GetType().Name} instead of {type.Name} for {doNotPopulateThisValue.GetStringOr("the delegate")}");
        }

        Fail($"No exception was thrown by {doNotPopulateThisValue.GetStringOr("the delegate")}");

        return null;
    }

    public static Exception Throws(Action @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => Throws<Exception>(@delegate, doNotPopulateThisValue);

    public static Exception Throws(Type type, Action @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
    {
        try
        {
            @delegate();
        }
        catch (Exception e) when (type.IsAssignableFrom(e.GetType()))
        {
            return e;
        }
        catch (Exception e)
        {
            Fail($"Exception is of type {e.GetType().Name} instead of {type.Name} for {doNotPopulateThisValue.GetStringOr("the delegate")}");
        }

        Fail($"No exception was thrown by {doNotPopulateThisValue.GetStringOr("the delegate")}");

        return null;
    }

    public static TException Throws<TException>(Action @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
    {
        return (TException)Throws(typeof(TException), @delegate, doNotPopulateThisValue);
    }

    public static TException Throws<TException>(string parameterName, Action @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : ArgumentException
    {
        var ex = Throws<TException>(@delegate, doNotPopulateThisValue);

        if (ex.ParamName?.Equals(parameterName, StringComparison.Ordinal) == false)
        {
            Fail($"Incorrect parameter name {new StringDifference(ex.ParamName, parameterName).ToString("it differs at index")}");
        }

        return ex;
    }

    [DoesNotReturn]
    public static void Fail(string reason) => TUnit.Assertions.Fail.Test(reason);
}