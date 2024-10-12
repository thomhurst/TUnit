using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateSource<TActual>(AssertionBuilder<TActual> assertionBuilder) : IDelegateSource<TActual>
{
    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder { get; } = assertionBuilder;

    public ThrowsException<TActual, TException> Throws<TException>()
        where TException : Exception
    {
        return new ThrowsException<TActual, TException>(
            this.RegisterAssertion(new ThrowsOfTypeAssertCondition<TActual, TException>(), [], $"{nameof(Throws)}<{typeof(TException).Name}>"),
            this,
            e => e);
    }

    public ThrowsException<TActual, TException> ThrowsExactly<TException>()
        where TException : Exception
    {
        return new ThrowsException<TActual, TException>(
            this.RegisterAssertion(new ThrowsExactTypeOfDelegateAssertCondition<TActual, TException>(), [], $"{nameof(ThrowsExactly)}<{typeof(TException).Name}>"),
            this,
            e => e);
    }

    public ThrowsException<TActual, Exception> ThrowsException()
    {
        return new ThrowsException<TActual, Exception>(
            this.RegisterAssertion(new ThrowsAnyExceptionAssertCondition<TActual>(), []),
            this,
            e => e);
    }

    public CastableAssertionBuilder<TActual, TActual> ThrowsNothing()
    {
        return new CastableAssertionBuilder<TActual, TActual>(
            this.RegisterAssertion(new ThrowsNothingAssertCondition<TActual>(), []),
            assertionData => assertionData.Result);
    }
}