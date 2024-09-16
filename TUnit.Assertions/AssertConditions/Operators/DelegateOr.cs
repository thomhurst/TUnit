using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateOr<TActual> 
    : Or<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>, IDelegateAssertions<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>, IOr<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>
{
    private readonly IAssertionResultProvider<TActual> _assertionResultProvider;
    private readonly AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> _assertionBuilder;

    public DelegateOr(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> assertionBuilder)
    {
        _assertionResultProvider = assertionResultProvider;
        _assertionBuilder = assertionBuilder;
    }

    AssertionConnector<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> IAssertionConnector<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>.AssertionConnector => new(_assertionBuilder, ChainType.Or);
    
    public static DelegateOr<TActual> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> assertionBuilder)
    {
        return new DelegateOr<TActual>(assertionResultProvider, assertionBuilder);
    }
}