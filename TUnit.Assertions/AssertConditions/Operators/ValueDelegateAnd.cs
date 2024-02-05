namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateAnd<TActual> 
    : And<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>>, 
        IDelegateAssertions<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>>, 
        IAnd<ValueDelegateAnd<TActual?>, TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>>
{
    public ValueDelegateAnd(BaseAssertCondition<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> otherAssertCondition) : base(otherAssertCondition)
    {
    }
    
    public Is<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> Is => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    public Has<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> Has => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    public Does<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> Does => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    
    public Throws<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> Throws => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);
    
    public static ValueDelegateAnd<TActual?> Create(BaseAssertCondition<TActual?, ValueDelegateAnd<TActual?>, ValueDelegateOr<TActual?>> otherAssertCondition)
    {
        return new ValueDelegateAnd<TActual?>(otherAssertCondition);
    }
}