using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Connectors;

namespace TUnit.Assertions;

public abstract class Connector<T>
{
    public ConnectorType ConnectorType { get; }
    public BaseAssertCondition<T>? OtherAssertCondition { get; }

    public Connector(ConnectorType connectorType, BaseAssertCondition<T>? otherAssertCondition)
    {
        ConnectorType = connectorType;
        OtherAssertCondition = otherAssertCondition;
    }

    public BaseAssertCondition<T> Wrap(BaseAssertCondition<T> assertCondition)
    {
        return ConnectorType switch
        {
            ConnectorType.None => assertCondition,
            ConnectorType.And => new AssertConditionAnd<T>(OtherAssertCondition!, assertCondition),
            ConnectorType.Or => new AssertConditionAnd<T>(OtherAssertCondition!, assertCondition),
            _ => throw new ArgumentOutOfRangeException(nameof(ConnectorType), ConnectorType, "Unknown connector type")
        };
    }
}