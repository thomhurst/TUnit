using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateOr<TActual> 
    : Or<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, IOr<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>,
        IDelegateSource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>,
        IValueSource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
 {
     private readonly AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> _assertionBuilder;

     public ValueDelegateOr(AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
     {
         _assertionBuilder = assertionBuilder;
     }
    
     
    public static ValueDelegateOr<TActual> Create(AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
    {
        return new ValueDelegateOr<TActual>(assertionBuilder);
    }
    
    AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
        ISource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.AssertionBuilder => new OrAssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>(_assertionBuilder);
 }