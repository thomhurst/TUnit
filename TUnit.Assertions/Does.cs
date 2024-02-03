using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public class Does<T> : Connector<T>
{
    protected internal AssertionBuilder<T> AssertionBuilder { get; }

    public Does(AssertionBuilder<T> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<T>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }

  
}