using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateOr<TActual> 
    : Or<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IValueAssertions<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IDelegateAssertions<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IOr<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
 {
     private readonly IAssertionResultProvider<TActual> _assertionResultProvider;
     private readonly AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> _assertionBuilder;

     public ValueDelegateOr(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
     {
         _assertionResultProvider = assertionResultProvider;
         _assertionBuilder = assertionBuilder;
     }
    
    AssertionConnector<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> IAssertionBuilderProvider<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.AssertionConnector => new(_assertionBuilder, ChainType.Or);
     
    public static ValueDelegateOr<TActual> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate,
        AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
    {
        return new ValueDelegateOr<TActual>(assertionResultProvider, assertionBuilder);
    }
}