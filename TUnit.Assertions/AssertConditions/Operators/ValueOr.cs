using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueOr<TActual> 
    : Or<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IValueAssertions<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IOr<TActual, ValueAnd<TActual>, ValueOr<TActual>>
{
    private readonly IAssertionResultProvider<TActual> _assertionResultProvider;
    private readonly AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> _assertionBuilder;

    public ValueOr(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> assertionBuilder)
    {
        _assertionResultProvider = assertionResultProvider;
        _assertionBuilder = assertionBuilder;
    }

    AssertionConnector<TActual, ValueAnd<TActual>, ValueOr<TActual>> IAssertionBuilderProvider<TActual, ValueAnd<TActual>, ValueOr<TActual>>.AssertionConnector => new(_assertionBuilder, ChainType.Or);
  
    public static ValueOr<TActual> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> assertionBuilder)
    {
        return new ValueOr<TActual>(assertionResultProvider, assertionBuilder);
    }
}