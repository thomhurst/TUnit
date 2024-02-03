using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public class IsNot<TActual> : NotConnector<TActual>
{
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public IsNot(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }
    
    public BaseAssertCondition<TActual> EqualTo(TActual expected) => Invert(new EqualsAssertCondition<TActual>(AssertionBuilder, expected),
        (actual, exception) => $"Expected {actual} to equal {expected}");

    public BaseAssertCondition<TActual> Null => Invert(new NullAssertCondition<TActual>(AssertionBuilder),
        (actual, exception) => $"Expected {actual} to be null");

    public BaseAssertCondition<TActual> TypeOf<TExpected>() => Invert(new TypeOfAssertCondition<TActual, TExpected>(AssertionBuilder),
        (actual, exception) => $"Expected {actual} to not be of type {typeof(TExpected)}");
}