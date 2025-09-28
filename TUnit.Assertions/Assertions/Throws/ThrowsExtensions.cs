using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Extensions;

public static class ThrowsExtensions
{
    public static ThrowsException<object?, TException> Throws<TException>(this IDelegateSource delegateSource)
        where TException : Exception
    {
        delegateSource.RegisterAssertion(new ThrowsOfTypeAssertCondition<object?, TException>(), [], $"{nameof(Throws)}<{typeof(TException).Name}>");
        return new ThrowsException<object?, TException>(delegateSource, e => e);
    }

    public static ThrowsException<object?, Exception> Throws(this IDelegateSource delegateSource, Type type, [CallerArgumentExpression("type")] string? doNotPopulateThisValue = null)
    {
        delegateSource.RegisterAssertion(new ThrowsOfTypeAssertCondition(type), [doNotPopulateThisValue]);
        return new ThrowsException<object?, Exception>(delegateSource, e => e);
    }

    public static ThrowsException<object?, TException> ThrowsExactly<TException>(this IDelegateSource delegateSource)
        where TException : Exception
    {
        delegateSource.RegisterAssertion(new ThrowsExactTypeOfDelegateAssertCondition<object?, TException>(), [], $"{nameof(ThrowsExactly)}<{typeof(TException).Name}>");
        return new ThrowsException<object?, TException>(delegateSource, e => e);
    }

    public static ThrowsException<object?, Exception> ThrowsWithin(this IDelegateSource delegateSource, TimeSpan timeSpan, [CallerArgumentExpression("timeSpan")] string? doNotPopulateThisValue = null)
    {
        delegateSource.RegisterAssertion(new ThrowsWithinAssertCondition<object?, Exception>(timeSpan), [doNotPopulateThisValue]);
        return new ThrowsException<object?, Exception>(delegateSource, e => e);
    }

    public static ThrowsException<object?, TException> ThrowsWithin<TException>(this IDelegateSource delegateSource, TimeSpan timeSpan, [CallerArgumentExpression("timeSpan")] string? doNotPopulateThisValue = null)
        where TException : Exception
    {
        delegateSource.RegisterAssertion(new ThrowsWithinAssertCondition<object?, TException>(timeSpan), [doNotPopulateThisValue], $"{nameof(ThrowsWithin)}<{typeof(TException).Name}>");
        return new ThrowsException<object?, TException>(delegateSource, e => e);
    }

    public static ThrowsException<object?, Exception> ThrowsException(this IDelegateSource delegateSource)
    {
        delegateSource.RegisterAssertion(new ThrowsAnyExceptionAssertCondition<object?>(), []);
        return new ThrowsException<object?, Exception>(delegateSource, e => e);
    }

    public static AssertionBuilder<object?> ThrowsNothing(this IDelegateSource delegateSource)
    {
        return delegateSource.RegisterAssertion(new ThrowsNothingAssertCondition<object?>(), []);
    }

    public static AssertionBuilder<TActual> ThrowsNothing<TActual>(this IValueDelegateSource<TActual> delegateSource)
    {
        IValueSource<TActual> valueSource = delegateSource;
        return valueSource.RegisterAssertion(new ThrowsNothingAssertCondition<TActual>(), []);
    }

    public static ThrowsException<TActual, TException> WithParameterName<TActual, TException>(this ThrowsException<TActual, TException> throwsException, string expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
        where TException : ArgumentException
    {
        throwsException.RegisterAssertion((selector) => new ThrowsWithParamNameAssertCondition<TActual, TException>(expected, StringComparison.Ordinal, ex => selector(ex) as ArgumentException), [doNotPopulateThisValue]);
        return throwsException;
    }
}
