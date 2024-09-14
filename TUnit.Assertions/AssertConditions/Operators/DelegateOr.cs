using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateOr<TActual> 
    : Or<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>, IDelegateAssertions<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>, IOr<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>
{
    public DelegateOr(BaseAssertCondition<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> otherAssertCondition) : base(otherAssertCondition)
    {
    }

    Throws<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> IThrows<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>.Throws() => new(OtherAssertCondition.AssertionBuilder, ConnectorType.Or, OtherAssertCondition);
    
    public static DelegateOr<TActual> Create(BaseAssertCondition<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> otherAssertCondition)
    {
        return new DelegateOr<TActual>(otherAssertCondition);
    }
}