using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class DelegateAssertionBuilder
    : AssertionBuilder<object?>,
        IDelegateSource<object?>
{
    internal DelegateAssertionBuilder(Action action, string expressionBuilder) : base(action.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
    public ThrowsException<object?, TException> Throws<TException>() where TException : Exception
    {
        return new DelegateSource<object?>(this).Throws<TException>();
    }

    public ThrowsException<object?, TException> ThrowsExactly<TException>() where TException : Exception
    {
        return new DelegateSource<object?>(this).ThrowsExactly<TException>();
    }

    public ThrowsException<object?, Exception> ThrowsException()
    {
        return new DelegateSource<object?>(this).ThrowsException();
    }

    public CastableAssertionBuilder<object?, object?> ThrowsNothing()
    {
        return new DelegateSource<object?>(this).ThrowsNothing();
    }

    AssertionBuilder<object?> ISource<object?>.AssertionBuilder => this;
}