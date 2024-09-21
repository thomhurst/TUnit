using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueAnd<TActual> 
    : IValueSource<TActual>
{
    private readonly AssertionBuilder<TActual> _assertionBuilder;

    public ValueAnd(AssertionBuilder<TActual> assertionBuilder)
    {
        _assertionBuilder = assertionBuilder;
    }
    
    public static ValueAnd<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueAnd<TActual>(assertionBuilder);
    }

    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => new AndAssertionBuilder<TActual>(_assertionBuilder);
}