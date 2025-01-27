using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public static class ThrowsExtensions
{
    public static ThrowsException<object?, TException> Throws<TException>(this IDelegateSource<object?> delegateSource)
        where TException : Exception
    {
        return new ThrowsException<object?, TException>(
            delegateSource.RegisterAssertion(new ThrowsOfTypeAssertCondition<object?, TException>(), [], $"{nameof(Throws)}<{typeof(TException).Name}>"),
            delegateSource,
            e => e);
    }

    public static ThrowsException<object?, TException> ThrowsExactly<TException>(this IDelegateSource<object?> delegateSource)
        where TException : Exception
    {
        return new ThrowsException<object?, TException>(
            delegateSource.RegisterAssertion(new ThrowsExactTypeOfDelegateAssertCondition<object?, TException>(), [], $"{nameof(ThrowsExactly)}<{typeof(TException).Name}>"),
            delegateSource,
            e => e);
    }

    public static ThrowsException<object?, Exception> ThrowsException(this IDelegateSource<object?> delegateSource)
    {
        return new ThrowsException<object?, Exception>(
            delegateSource.RegisterAssertion(new ThrowsAnyExceptionAssertCondition<object?>(), []),
            delegateSource,
            e => e);
    }

    public static CastableAssertionBuilder<object?, object?> ThrowsNothing(this IDelegateSource<object?> delegateSource)
    {
        return new CastableAssertionBuilder<object?, object?>(
            delegateSource.RegisterAssertion(new ThrowsNothingAssertCondition<object?>(), []),
            assertionData => assertionData.Result);
    }
}