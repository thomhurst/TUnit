using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueAnd<TActual> 
    : And<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IValueAssertions<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IAnd<TActual, ValueAnd<TActual>, ValueOr<TActual>>
{
    public ValueAnd(BaseAssertCondition<TActual, ValueAnd<TActual>, ValueOr<TActual>> otherAssertCondition) : base(otherAssertCondition)
    {
    }

    Is<TActual, ValueAnd<TActual>, ValueOr<TActual>> IIs<TActual, ValueAnd<TActual>, ValueOr<TActual>>.Is() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    IsNot<TActual, ValueAnd<TActual>, ValueOr<TActual>> IIs<TActual, ValueAnd<TActual>, ValueOr<TActual>>.IsNot() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    Has<TActual, ValueAnd<TActual>, ValueOr<TActual>> IHas<TActual, ValueAnd<TActual>, ValueOr<TActual>>.Has() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    Does<TActual, ValueAnd<TActual>, ValueOr<TActual>> IDoes<TActual, ValueAnd<TActual>, ValueOr<TActual>>.Does() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    DoesNot<TActual, ValueAnd<TActual>, ValueOr<TActual>> IDoes<TActual, ValueAnd<TActual>, ValueOr<TActual>>.DoesNot() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    
    public static ValueAnd<TActual> Create(BaseAssertCondition<TActual, ValueAnd<TActual>, ValueOr<TActual>> otherAssertCondition)
    {
        return new ValueAnd<TActual>(otherAssertCondition);
    }
}