using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueAnd<TActual> 
    : And<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IValueAssertions<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IAnd<TActual, ValueAnd<TActual>, ValueOr<TActual>>
{
    private readonly IAssertionResultProvider<TActual> _assertionResultProvider;
    private readonly AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> _assertionBuilder;

    public ValueAnd(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> assertionBuilder)
    {
        _assertionResultProvider = assertionResultProvider;
        _assertionBuilder = assertionBuilder;
    }

    AssertionConnector<TActual, ValueAnd<TActual>, ValueOr<TActual>> IAssertionBuilderProvider<TActual, ValueAnd<TActual>, ValueOr<TActual>>.AssertionConnector => new(_assertionBuilder, ChainType.Or);
  
    public static ValueAnd<TActual> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> assertionBuilder)
    {
        return new ValueAnd<TActual>(assertionResultProvider, assertionBuilder);
    }
}