using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueAnd<TActual> 
    : And<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IValueAssertions<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IAnd<TActual, ValueAnd<TActual>, ValueOr<TActual>>
    where TOutputs : AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IInvokableAssertionBuilder
{
    private readonly Func<Task<AssertionData<TActual>>> _assertionDataDelegate;
    private readonly AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> _assertionBuilder;

    public ValueAnd(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> assertionBuilder)
    {
        _assertionDataDelegate = assertionDataDelegate;
        _assertionBuilder = assertionBuilder;
    }
    
    public static ValueAnd<TActual> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> assertionBuilder)
    {
        return new ValueAnd<TActual>(assertionDataDelegate, assertionBuilder);
    }
}