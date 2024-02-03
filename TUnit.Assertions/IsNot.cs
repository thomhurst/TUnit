using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public class IsNot<TActual> : Connector<TActual>
{
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public IsNot(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }

    public BaseAssertCondition<TActual> Null => Wrap(new NotNullAssertCondition<TActual>(AssertionBuilder));

    public BaseAssertCondition<TActual> TypeOf<TExpected>() => Wrap(new NotTypeOfAssertCondition<TActual, TExpected>(AssertionBuilder));
}