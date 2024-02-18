using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions;

public class Does<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }

    public Does(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder
            .AppendConnector(connectorType)
            .AppendExpression("Does");
    }

    public DoesNot<TActual, TAnd, TOr> Not => new(AssertionBuilder, ConnectorType, OtherAssertCondition);
}