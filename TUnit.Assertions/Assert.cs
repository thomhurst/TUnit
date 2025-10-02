using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;
using TUnit.Assertions.Wrappers;

namespace TUnit.Assertions;

[UnconditionalSuppressMessage("Usage", "TUnitAssertions0002:Assert statements must be awaited")]
public static class Assert
{
    public static ValueAssertion<TActual> That<TActual>(TActual value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new ValueAssertion<TActual>(value, doNotPopulateThisValue);
    }

    public static ValueAssertion<IEnumerable<object>> That(IEnumerable enumerable, [CallerArgumentExpression(nameof(enumerable))] string? doNotPopulateThisValue = null)
    {
        return new ValueAssertion<IEnumerable<object>>(new UnTypedEnumerableWrapper(enumerable), doNotPopulateThisValue);
    }

    public static DelegateAssertion That(Action value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new DelegateAssertion(value, doNotPopulateThisValue);
    }

    public static ValueDelegateAssertion<TActual> That<TActual>(Func<TActual> value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new ValueDelegateAssertion<TActual>(value, doNotPopulateThisValue);
    }

    public static AsyncDelegateAssertion That(Func<Task> value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AsyncDelegateAssertion(value, doNotPopulateThisValue);
    }

    public static AsyncValueDelegateAssertion<TActual> That<TActual>(Func<Task<TActual>> value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AsyncValueDelegateAssertion<TActual>(value, doNotPopulateThisValue);
    }

    public static AsyncDelegateAssertion That(Task value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AsyncDelegateAssertion(async () => await value, doNotPopulateThisValue);
    }

    public static AsyncValueDelegateAssertion<TActual> That<TActual>(Task<TActual> value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AsyncValueDelegateAssertion<TActual>(async () => await value, doNotPopulateThisValue);
    }

    public static AsyncDelegateAssertion That(ValueTask value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AsyncDelegateAssertion(async () => await value, doNotPopulateThisValue);
    }

    public static AsyncValueDelegateAssertion<TActual> That<TActual>(ValueTask<TActual> value, [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new AsyncValueDelegateAssertion<TActual>(async () => await value, doNotPopulateThisValue);
    }

    public static IDisposable Multiple()
    {
        return new AssertionScope();
    }

    public static ThrowsException<object?, Exception> ThrowsAsync(Func<Task> @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync<Exception>(@delegate, doNotPopulateThisValue);

    public static ThrowsException<object?, Exception> ThrowsAsync(Task @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync(async () => await @delegate, doNotPopulateThisValue);

    public static ThrowsException<object?, Exception> ThrowsAsync(ValueTask @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync(async () => await @delegate, doNotPopulateThisValue);

    public static ThrowsException<object?, TException> ThrowsAsync<TException>(Task @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
        => ThrowsAsync<TException>(async () => await @delegate, doNotPopulateThisValue);

    public static ThrowsException<object?, TException> ThrowsAsync<TException>(ValueTask @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
        => ThrowsAsync<TException>(async () => await @delegate, doNotPopulateThisValue);

    public static ThrowsException<object?, TException> ThrowsAsync<TException>(Func<Task> @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
    {
        var throwsException = ThrowsAsync(typeof(TException), @delegate, doNotPopulateThisValue);

        return Unsafe.As<ThrowsException<object?, TException>>(throwsException);
    }

    public static ThrowsException<object?, Exception> ThrowsAsync(Type type, Task @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync(type, async () => await @delegate, doNotPopulateThisValue);

    public static ThrowsException<object?, Exception> ThrowsAsync(Type type, ValueTask @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        => ThrowsAsync(type, async () => await @delegate, doNotPopulateThisValue);

    public static ThrowsException<object?, Exception> ThrowsAsync(Type type, Func<Task> @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
    {
        return That(@delegate, doNotPopulateThisValue).Throws(type);
    }

    public static ThrowsException<object?, TException> ThrowsAsync<TException>(string parameterName,
        Task @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null,
        [CallerArgumentExpression(nameof(parameterName))] string? doNotPopulateThisValue2 = null) where TException : ArgumentException
        => ThrowsAsync<TException>(parameterName, async () => await @delegate, doNotPopulateThisValue, doNotPopulateThisValue2);

    public static ThrowsException<object?, TException> ThrowsAsync<TException>(string parameterName,
        ValueTask @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null,
        [CallerArgumentExpression(nameof(parameterName))] string? doNotPopulateThisValue2 = null) where TException : ArgumentException
        => ThrowsAsync<TException>(parameterName, async () => await @delegate, doNotPopulateThisValue, doNotPopulateThisValue2);

    public static ThrowsException<object?, TException> ThrowsAsync<TException>(string parameterName,
        Func<Task> @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null,
        [CallerArgumentExpression(nameof(parameterName))] string? doNotPopulateThisValue2 = null) where TException : ArgumentException
    {
        return That(@delegate, doNotPopulateThisValue).Throws<TException>().WithParameterName(parameterName, doNotPopulateThisValue2);
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

        return null!;
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

    public static TException Throws<TException>(Action @delegate, [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null) where TException : Exception
    {
        return (TException) Throws(typeof(TException), @delegate, doNotPopulateThisValue);
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
