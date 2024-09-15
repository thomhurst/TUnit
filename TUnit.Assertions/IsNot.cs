using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions;

public class IsNot<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }
    
    public IsNot(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder.AppendConnector(connectorType);
    }
}