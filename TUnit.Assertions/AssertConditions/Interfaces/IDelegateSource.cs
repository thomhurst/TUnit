using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface IDelegateSource<TActual> : ISource<TActual>
{
    public CastableAssertionBuilder<TActual, TActual> ThrowsNothing();
    public ThrowsException<TActual, Exception> ThrowsException();
    public ThrowsException<TActual, TException> Throws<TException>() where TException : Exception;
    public ThrowsException<TActual, TException> ThrowsExactly<TException>() where TException : Exception;

}