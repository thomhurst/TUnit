using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateOr<TActual> 
    : Or<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>, IOr<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>,
        IDelegateSource<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>
{
    private readonly AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> _assertionBuilder;

    public DelegateOr(AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> assertionBuilder)
    {
        _assertionBuilder = assertionBuilder;
    }
    
    public static DelegateOr<TActual> Create(AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> assertionBuilder)
    {
        return new DelegateOr<TActual>(assertionBuilder);
    }

    AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> ISource<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>.AssertionBuilder => new OrAssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>(_assertionBuilder);
}