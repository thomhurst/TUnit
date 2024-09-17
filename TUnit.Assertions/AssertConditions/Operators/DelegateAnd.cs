using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateAnd<TActual> 
    : And<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>, IAnd<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>,
        IDelegateSource<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>
{
    private readonly AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> _assertionBuilder;

    public DelegateAnd(AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> assertionBuilder)
    {
        _assertionBuilder = assertionBuilder;
    }

    public static DelegateAnd<TActual> Create(AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> assertionBuilder)
    {
        return new DelegateAnd<TActual>(assertionBuilder);
    }
    
    AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> ISource<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>.AssertionBuilder => new AndAssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>(_assertionBuilder);
}