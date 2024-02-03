using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public abstract class NotConnector<T> : Connector<T>
{
    protected NotConnector(ConnectorType connectorType, BaseAssertCondition<T>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
    }

    protected BaseAssertCondition<T> Invert(BaseAssertCondition<T> assertCondition, Func<T?, Exception?, string> messageFactory)
    {
        return Wrap(assertCondition.Invert(messageFactory));
    }
}