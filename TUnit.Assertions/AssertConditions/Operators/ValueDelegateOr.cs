using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateOr<TActual>(AssertionBuilder<TActual> assertionBuilder) : IValueDelegateSource<TActual>
{
    public static ValueDelegateOr<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueDelegateOr<TActual>(assertionBuilder);
    }
    
    AssertionBuilder<TActual>
        ISource<TActual>.AssertionBuilder => new OrAssertionBuilder<TActual>(assertionBuilder);
 }