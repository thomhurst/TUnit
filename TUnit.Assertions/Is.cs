using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public class Is<TActual> : Connector<TActual>
{
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }

    public Is(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType,
        BaseAssertCondition<TActual>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }

    public BaseAssertCondition<TActual> EqualTo(TActual expected)
    {
        return Wrap(new EqualsAssertCondition<TActual, TActual>(AssertionBuilder, expected));
    }

    public BaseAssertCondition<TActual> SameReference(TActual expected)
    {
        return Wrap(new SameReferenceAssertCondition<TActual, TActual>(AssertionBuilder, expected));
    }

    public BaseAssertCondition<TActual> Null => Wrap(new NullAssertCondition<TActual>(AssertionBuilder));

    public BaseAssertCondition<TActual> TypeOf<TExpected>() => Wrap(new TypeOfAssertCondition<TActual, TExpected>(AssertionBuilder));

    public IsNot<TActual> Not => new(AssertionBuilder, ConnectorType, OtherAssertCondition);
}