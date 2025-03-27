using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Exceptions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Throws;
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
    
    public static InvokableDelegateAssertionBuilder ThrowsWithin(this IDelegateSource delegateSource, TimeSpan timeSpan, [CallerArgumentExpression("timeSpan")] string? doNotPopulateThisValue = null) 
    {
        return delegateSource.RegisterAssertion(new ThrowsWithinAssertCondition<object?, Exception>(timeSpan), [doNotPopulateThisValue]);
    }
    
    public static InvokableDelegateAssertionBuilder ThrowsWithin<TException>(this IDelegateSource delegateSource, TimeSpan timeSpan, [CallerArgumentExpression("timeSpan")] string? doNotPopulateThisValue = null)
        where TException : Exception
    {
        return delegateSource.RegisterAssertion(new ThrowsWithinAssertCondition<object?, TException>(timeSpan), [doNotPopulateThisValue],
            $"{nameof(ThrowsWithin)}<{typeof(TException).Name}>");
    }

    public static ThrowsException<object?, Exception> ThrowsException(this IDelegateSource delegateSource)
    {
        return new ThrowsException<object?, Exception>(
            delegateSource.RegisterAssertion(new ThrowsAnyExceptionAssertCondition<object?>(), []),
            delegateSource,
            e => e);
    }

    public static CastableResultAssertionBuilder<object?, object?> ThrowsNothing(this IDelegateSource delegateSource)
    {
        return new CastableResultAssertionBuilder<object?, object?>(
            delegateSource.RegisterAssertion(new ThrowsNothingAssertCondition<object?>(), []));
    }

    public static CastableResultAssertionBuilder<TActual, TActual> ThrowsNothing<TActual>(this IValueDelegateSource<TActual> delegateSource)
    {
        IValueSource<TActual> valueSource = delegateSource;
        return new CastableResultAssertionBuilder<TActual, TActual>(
            valueSource.RegisterAssertion(new ThrowsNothingAssertCondition<TActual>(), []));
    }

    public static ThrowsException<TActual, TException> WithParameterName<TActual, TException>(this ThrowsException<TActual, TException> throwsException, string expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
        where TException : ArgumentException
    {
        throwsException.RegisterAssertion((selector) => new ThrowsWithParamNameAssertCondition<TActual, TException>(expected, StringComparison.Ordinal, ex => selector(ex) as ArgumentException), [doNotPopulateThisValue]);
        return throwsException;
    }
}