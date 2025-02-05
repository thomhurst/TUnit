using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Exceptions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public static class ThrowsExtensions
{
    public static ThrowsException<object?, TException> Throws<TException>(this IDelegateSource delegateSource)
        where TException : Exception
    {
        return new ThrowsException<object?, TException>(
            delegateSource.RegisterAssertion(new ThrowsOfTypeAssertCondition<object?, TException>(), [], $"{nameof(Throws)}<{typeof(TException).Name}>"),
            delegateSource,
            e => e);
    }

    public static ThrowsException<object?, TException> ThrowsExactly<TException>(this IDelegateSource delegateSource)
        where TException : Exception
    {
        return new ThrowsException<object?, TException>(
            delegateSource.RegisterAssertion(new ThrowsExactTypeOfDelegateAssertCondition<object?, TException>(), [], $"{nameof(ThrowsExactly)}<{typeof(TException).Name}>"),
            delegateSource,
            e => e);
    }

    public static ThrowsException<object?, Exception> ThrowsException(this IDelegateSource delegateSource)
    {
        return new ThrowsException<object?, Exception>(
            delegateSource.RegisterAssertion(new ThrowsAnyExceptionAssertCondition<object?>(), []),
            delegateSource,
            e => e);
    }

    public static CastableAssertionBuilder<object?, object?> ThrowsNothing(this IDelegateSource delegateSource)
    {
        return new CastableAssertionBuilder<object?, object?>(
            delegateSource.RegisterAssertion(new ThrowsNothingAssertCondition<object?>(), []),
            assertionData => assertionData.Result);
    }

    public static CastableAssertionBuilder<TActual, TActual> ThrowsNothing<TActual>(this IValueDelegateSource<TActual> delegateSource)
    {
        IValueSource<TActual> valueSource = delegateSource;
        return new CastableAssertionBuilder<TActual, TActual>(
            valueSource.RegisterAssertion(new ThrowsNothingAssertCondition<TActual>(), []),
            assertionData => assertionData.Result is TActual actual ? actual : default);
    }

    public static ThrowsException<TActual, TException> WithParameterName<TActual, TException>(this ThrowsException<TActual, TException> throwsException, string expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
        where TException : ArgumentException
    {
        throwsException.RegisterAssertion((selector) => new ThrowsWithParamNameAssertCondition<TActual, TException>(expected, StringComparison.Ordinal, ex => selector(ex) as ArgumentException), [doNotPopulateThisValue]);
        return throwsException;
    }
}