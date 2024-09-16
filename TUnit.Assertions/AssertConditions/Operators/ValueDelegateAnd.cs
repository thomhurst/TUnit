using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateAnd<TActual> 
    : And<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IValueAssertions<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IDelegateAssertions<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IAnd<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
{
    private readonly IAssertionResultProvider<TActual> _assertionResultProvider;
    private readonly AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> _assertionBuilder;

    public ValueDelegateAnd(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
    {
        _assertionResultProvider = assertionResultProvider;
        _assertionBuilder = assertionBuilder;
    }
    
    public static ValueDelegateAnd<TActual> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
    {
        return new ValueDelegateAnd<TActual>(assertionResultProvider, assertionBuilder);
    }

    AssertionConnector<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IAssertionConnector<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.AssertionConnector => new(_assertionBuilder, ChainType.Or);
}