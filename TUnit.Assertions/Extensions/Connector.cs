using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;

public abstract class Connector<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    public ConnectorType ConnectorType { get; }
    public BaseAssertCondition<TActual, TAnd, TOr>? OtherAssertCondition { get; }

    public Connector(ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition)
    {
        ConnectorType = connectorType;
        OtherAssertCondition = otherAssertCondition;
    }

    public BaseAssertCondition<TActual, TAnd, TOr> Wrap(BaseAssertCondition<TActual, TAnd, TOr> assertCondition)
    {
        return ConnectorType switch
        {
            ConnectorType.None => assertCondition,
            ConnectorType.And => new AssertConditionAnd<TActual, TAnd, TOr>(OtherAssertCondition!, assertCondition),
            ConnectorType.Or => new AssertConditionAnd<TActual, TAnd, TOr>(OtherAssertCondition!, assertCondition),
            _ => throw new ArgumentOutOfRangeException(nameof(ConnectorType), ConnectorType, "Unknown connector type")
        };
    }
}