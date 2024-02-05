namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateAnd<TActual> 
    : And<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>, 
        IDelegateAssertions<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>, 
        IAnd<DelegateAnd<TActual>, TActual, DelegateAnd<TActual>, DelegateOr<TActual>>
{
    public DelegateAnd(BaseAssertCondition<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> otherAssertCondition) : base(otherAssertCondition)
    {
    }
    
    public Throws<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> Throws => new(OtherAssertCondition.AssertionBuilder, ConnectorType.And, OtherAssertCondition);

    public static DelegateAnd<TActual> Create(BaseAssertCondition<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> otherAssertCondition)
    {
        return new DelegateAnd<TActual>(otherAssertCondition);
    }
}