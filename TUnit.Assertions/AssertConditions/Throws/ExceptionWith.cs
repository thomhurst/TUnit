using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ExceptionWith<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }
    
    public ExceptionWith(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition, Func<Exception?, Exception?> exceptionSelector, [CallerMemberName] string callerMemberName = "") : base(connectorType, otherAssertCondition)
    {
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = assertionBuilder
            .AppendExpression(callerMemberName);
    }

    public ExceptionWithMessage<TActual, TAnd, TOr> Message =>
        new(AssertionBuilder, ConnectorType, OtherAssertCondition, _exceptionSelector);
    
    public ThrowsException<TActual, TAnd, TOr> InnerException =>
        new(AssertionBuilder, ConnectorType, OtherAssertCondition, e => _exceptionSelector(e)?.InnerException);
}