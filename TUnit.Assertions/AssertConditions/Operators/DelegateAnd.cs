using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class DelegateAnd<TActual> 
    : IDelegateSource<TActual>
{
    private readonly AssertionBuilder<TActual> _assertionBuilder;

    public DelegateAnd(AssertionBuilder<TActual> assertionBuilder)
    {
        _assertionBuilder = assertionBuilder;
    }

    public static DelegateAnd<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new DelegateAnd<TActual>(assertionBuilder);
    }
    
    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => new AndAssertionBuilder<TActual>(_assertionBuilder);
}