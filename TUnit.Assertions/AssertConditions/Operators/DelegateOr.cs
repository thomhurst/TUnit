using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateOr<TActual> 
    : Or<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>, IOr<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>,
        IDelegateSource<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>
{
    private readonly Func<Task<AssertionData<TActual>>> _assertionDataDelegate;
    private readonly AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> _assertionBuilder;

    public DelegateOr(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> assertionBuilder)
    {
        _assertionDataDelegate = assertionDataDelegate;
        _assertionBuilder = assertionBuilder;
    }
    
    public static DelegateOr<TActual> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> assertionBuilder)
    {
        return new DelegateOr<TActual>(assertionDataDelegate, assertionBuilder);
    }

    AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> ISource<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>.AssertionBuilder => new OrAssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>(_assertionBuilder);
}