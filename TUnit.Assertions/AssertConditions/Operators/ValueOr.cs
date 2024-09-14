using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueOr<TActual> 
    : Or<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IValueAssertions<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IOr<TActual, ValueAnd<TActual>, ValueOr<TActual>>
{
    public ValueOr(BaseAssertCondition<TActual, ValueAnd<TActual>, ValueOr<TActual>> otherAssertCondition) : base(otherAssertCondition)
    {
    }

    Is<TActual, ValueAnd<TActual>, ValueOr<TActual>> IIs<TActual, ValueAnd<TActual>, ValueOr<TActual>>.Is() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    IsNot<TActual, ValueAnd<TActual>, ValueOr<TActual>> IIs<TActual, ValueAnd<TActual>, ValueOr<TActual>>.IsNot() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    Has<TActual, ValueAnd<TActual>, ValueOr<TActual>> IHas<TActual, ValueAnd<TActual>, ValueOr<TActual>>.Has() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    Does<TActual, ValueAnd<TActual>, ValueOr<TActual>> IDoes<TActual, ValueAnd<TActual>, ValueOr<TActual>>.Does() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    DoesNot<TActual, ValueAnd<TActual>, ValueOr<TActual>> IDoes<TActual, ValueAnd<TActual>, ValueOr<TActual>>.DoesNot() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    
    public static ValueOr<TActual> Create(BaseAssertCondition<TActual, ValueAnd<TActual>, ValueOr<TActual>> otherAssertCondition)
    {
        return new ValueOr<TActual>(otherAssertCondition);
    }
}