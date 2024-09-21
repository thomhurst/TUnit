using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ExceptionWith<TActual>
{
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public ExceptionWith(AssertionBuilder<TActual> assertionBuilder, Func<Exception?, Exception?> exceptionSelector, [CallerMemberName] string callerMemberName = "")
    {
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = assertionBuilder
            .AppendExpression(callerMemberName);
    }

    public ExceptionWithMessage<TActual> Message =>
        new(AssertionBuilder, _exceptionSelector);
    
    public ThrowsException<TActual> InnerException => new(AssertionBuilder, e => _exceptionSelector(e)?.InnerException);
}