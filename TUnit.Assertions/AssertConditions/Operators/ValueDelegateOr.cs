using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateOr<TActual> 
    : Or<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IOr<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
 {
     private readonly Func<Task<AssertionData<TActual>>> _assertionDataDelegate;
     private readonly AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> _assertionBuilder;

     public ValueDelegateOr(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
     {
         _assertionDataDelegate = assertionDataDelegate;
         _assertionBuilder = assertionBuilder;
     }
    
     
    public static ValueDelegateOr<TActual> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
    {
        return new ValueDelegateOr<TActual>(assertionDataDelegate, assertionBuilder);
    }
}