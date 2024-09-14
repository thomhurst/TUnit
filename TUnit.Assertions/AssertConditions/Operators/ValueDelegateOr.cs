using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateOr<TActual> 
    : Or<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IValueAssertions<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IDelegateAssertions<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IOr<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
 {
    public ValueDelegateOr(BaseAssertCondition<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> otherAssertCondition) : base(otherAssertCondition)
    {
    }
    
    Is<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IIs<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.Is() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    IsNot<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IIs<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.IsNot() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    Has<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IHas<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.Has() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    Does<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IDoes<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.Does() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    DoesNot<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IDoes<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.DoesNot() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    Throws<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IThrows<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.Throws() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    
    public static ValueDelegateOr<TActual> Create(BaseAssertCondition<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> otherAssertCondition)
    {
        return new ValueDelegateOr<TActual>(otherAssertCondition);
    }
}