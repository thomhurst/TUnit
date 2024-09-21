using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateAnd<TActual> : IValueDelegateSource<TActual>
{
    private readonly AssertionBuilder<TActual> _assertionBuilder;

    public ValueDelegateAnd(AssertionBuilder<TActual> assertionBuilder)
    {
        _assertionBuilder = assertionBuilder;
    }
    
    public static ValueDelegateAnd<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueDelegateAnd<TActual>(assertionBuilder);
    }

    AssertionBuilder<TActual>
        ISource<TActual>.AssertionBuilder => new AndAssertionBuilder<TActual>(_assertionBuilder);
}