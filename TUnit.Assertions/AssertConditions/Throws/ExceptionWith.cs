using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ExceptionWith<TActual>
{
    private readonly IDelegateSource<TActual> _delegateSource;
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public ExceptionWith(IDelegateSource<TActual> delegateSource, Func<Exception?, Exception?> exceptionSelector, [CallerMemberName] string callerMemberName = "")
    {
        _delegateSource = delegateSource;
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = delegateSource.AssertionBuilder
            .AppendExpression(callerMemberName);
    }

    public ExceptionWithMessage<TActual> Message =>
        new(_delegateSource, _exceptionSelector);
    
    public ThrowsException<TActual> InnerException => new(_delegateSource, e => _exceptionSelector(e)?.InnerException);
}