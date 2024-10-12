using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateAnd<TActual>(AssertionBuilder<TActual> assertionBuilder) : IDelegateSource<TActual>
{
    public static DelegateAnd<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new DelegateAnd<TActual>(assertionBuilder);
    }

    public ThrowsException<TActual, TException> Throws<TException>() where TException : Exception
    {
        return new DelegateSource<TActual>(assertionBuilder).Throws<TException>();
    }

    public ThrowsException<TActual, TException> ThrowsExactly<TException>() where TException : Exception
    {
        return new DelegateSource<TActual>(assertionBuilder).ThrowsExactly<TException>();
    }

    public ThrowsException<TActual, Exception> ThrowsException()
    {
        return new DelegateSource<TActual>(assertionBuilder).ThrowsException();
    }

    public CastableAssertionBuilder<TActual, TActual> ThrowsNothing()
    {
        return new DelegateSource<TActual>(assertionBuilder).ThrowsNothing();
    }

    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => new AndAssertionBuilder<TActual>(assertionBuilder);
}