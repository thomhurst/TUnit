using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ExceptionWith<TActual>(
    IDelegateSource<TActual> delegateSource,
    Func<Exception?, Exception?> exceptionSelector,
    [CallerMemberName] string callerMemberName = "")
{
    protected AssertionBuilder<TActual> AssertionBuilder { get; } = delegateSource.AssertionBuilder
        .AppendExpression(callerMemberName);

    public ExceptionWithMessage<TActual> Message =>
        new(delegateSource, exceptionSelector);
    
    public ThrowsException<TActual> InnerException => new(delegateSource, e => exceptionSelector(e)?.InnerException);
}