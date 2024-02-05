namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueAnd<TActual> 
    : And<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>>, IValueAssertions<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>>, 
        IAnd<ValueAnd<TActual?>, TActual?, ValueAnd<TActual?>, ValueOr<TActual?>>
{
    public ValueAnd(BaseAssertCondition<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>> otherAssertCondition) : base(otherAssertCondition)
    {
    }
    
    public Is<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>> Is => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    public Has<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>> Has => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    public Does<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>> Does => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    
    public static ValueAnd<TActual?> Create(BaseAssertCondition<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>> otherAssertCondition)
    {
        return new ValueAnd<TActual?>(otherAssertCondition);
    }
}