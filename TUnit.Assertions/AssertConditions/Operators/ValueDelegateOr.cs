using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateOr<TActual> : IValueDelegateSource<TActual>
 {
     private readonly AssertionBuilder<TActual> _assertionBuilder;

     public ValueDelegateOr(AssertionBuilder<TActual> assertionBuilder)
     {
         _assertionBuilder = assertionBuilder;
     }
    
     
    public static ValueDelegateOr<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueDelegateOr<TActual>(assertionBuilder);
    }
    
    AssertionBuilder<TActual>
        ISource<TActual>.AssertionBuilder => new OrAssertionBuilder<TActual>(_assertionBuilder);
 }