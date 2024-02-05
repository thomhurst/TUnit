namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueOr<TActual> 
    : Or<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>>, 
        IValueAssertions<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>>, 
        IOr<ValueOr<TActual?>, TActual?, ValueAnd<TActual?>, ValueOr<TActual?>>
    
{
    public ValueOr(BaseAssertCondition<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>> otherAssertCondition) : base(otherAssertCondition)
    {
    }
    
    public Is<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>> Is => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    public Has<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>> Has => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    public Does<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>> Does => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    
    public static ValueOr<TActual?> Create(BaseAssertCondition<TActual?, ValueAnd<TActual?>, ValueOr<TActual?>> otherAssertCondition)
    {
        return new ValueOr<TActual?>(otherAssertCondition);
    }
}