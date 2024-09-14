using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateAnd<TActual> 
    : And<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IValueAssertions<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IDelegateAssertions<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IAnd<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
{
    public ValueDelegateAnd(BaseAssertCondition<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> otherAssertCondition) : base(otherAssertCondition)
    {
    }
    
    public static ValueDelegateAnd<TActual> Create(BaseAssertCondition<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> otherAssertCondition)
    {
        return new ValueDelegateAnd<TActual>(otherAssertCondition);
    }

    Is<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IIs<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.Is() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    IsNot<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IIs<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.IsNot() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    Has<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IHas<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.Has() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    Does<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IDoes<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.Does() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    DoesNot<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IDoes<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.DoesNot() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    Throws<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IThrows<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.Throws() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
}