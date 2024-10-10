using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateAnd<TActual>(AssertionBuilder<TActual> assertionBuilder) : IDelegateSource<TActual>
{
    public static DelegateAnd<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new DelegateAnd<TActual>(assertionBuilder);
    }
    
    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => new AndAssertionBuilder<TActual>(assertionBuilder);
}