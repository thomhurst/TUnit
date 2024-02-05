namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateOr<TActual> 
    : Or<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>>, 
        IValueAssertions<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>>, 
        IDelegateAssertions<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>>, 
        IOr<ValueDelegateOr<TActual?>, TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>>
{
    public ValueDelegateOr(BaseAssertCondition<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> otherAssertCondition) : base(otherAssertCondition)
    {
    }
    
    public Is<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> Is => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    public Has<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> Has => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    public Does<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> Does => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    
    public Throws<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> Throws => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    
    public static ValueDelegateOr<TActual?> Create(BaseAssertCondition<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> otherAssertCondition)
    {
        return new ValueDelegateOr<TActual?>(otherAssertCondition);
    }
}