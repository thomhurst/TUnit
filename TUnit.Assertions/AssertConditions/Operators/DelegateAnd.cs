using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateAnd<TActual> 
    : And<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>, IDelegateAssertions<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>, IAnd<TActual, DelegateAnd<TActual>, DelegateOr<TActual>>
{
    private readonly IAssertionResultProvider<TActual> _assertionResultProvider;
    private readonly AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> _assertionBuilder;

    public DelegateAnd(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> assertionBuilder)
    {
        _assertionResultProvider = assertionResultProvider;
        _assertionBuilder = assertionBuilder;
    }

    public static DelegateAnd<TActual> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> assertionBuilder)
    {
        return new DelegateAnd<TActual>(assertionResultProvider, assertionBuilder);
    }

    public AssertionConnector<TActual, DelegateAnd<TActual>, DelegateOr<TActual>> AssertionConnector => new(_assertionBuilder, ChainType.Or);
}