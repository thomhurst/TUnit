using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Delegates;

public class Throws<TActual>
{
    private readonly IDelegateSource<TActual> _delegateSource;
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public Throws(IDelegateSource<TActual> delegateSource, Func<Exception?, Exception?> exceptionSelector, [CallerMemberName] string callerMemberName = "")
    {
        _delegateSource = delegateSource;
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = delegateSource.AssertionBuilder
            .AppendExpression($"{callerMemberName}()");
    }

    public CastableAssertionBuilder<TActual, TActual> Nothing()
    {
        return new CastableAssertionBuilder<TActual, TActual>(
            _delegateSource.RegisterAssertion(new ThrowsNothingAssertCondition<TActual>()
            , []),
            a => a);
    }

    public ThrowsException<TActual, Exception> Exception()
    {
        return new(_delegateSource.RegisterAssertion(new ThrowsAnyExceptionAssertCondition<TActual>()
            , []), _delegateSource, _exceptionSelector);
    }

    public ThrowsException<TActual, TException> Exactly<TException>() where TException : Exception
    {
        return new(_delegateSource.RegisterAssertion(new ThrowsExactTypeOfDelegateAssertCondition<TActual, TException>(), [], $"{nameof(Exactly)}<{typeof(TException).Name}>"), _delegateSource, _exceptionSelector);
    }

    public ThrowsException<TActual, TException> OfType<TException>() where TException : Exception
    {
        return new(_delegateSource.RegisterAssertion(new ThrowsOfTypeAssertCondition<TActual, TException>(), [], $"{nameof(OfType)}<{typeof(TException).Name}>"), _delegateSource, _exceptionSelector);
    }
}