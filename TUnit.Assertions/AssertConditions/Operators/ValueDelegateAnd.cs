using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateAnd<TActual> 
    : And<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IAnd<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
{
    private readonly Func<Task<AssertionData<TActual>>> _assertionDataDelegate;
    private readonly AssertionBuilder<TActual> _assertionBuilder;

    public ValueDelegateAnd(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual> assertionBuilder)
    {
        _assertionDataDelegate = assertionDataDelegate;
        _assertionBuilder = assertionBuilder;
    }
    
    public static ValueDelegateAnd<TActual> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
    {
        return new ValueDelegateAnd<TActual>(assertionDataDelegate, assertionBuilder);
    }
}