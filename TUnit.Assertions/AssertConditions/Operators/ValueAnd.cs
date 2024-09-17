using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueAnd<TActual> 
    : And<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IAnd<TActual, ValueAnd<TActual>, ValueOr<TActual>>,
        IValueSource<TActual, ValueAnd<TActual>, ValueOr<TActual>>
{
    private readonly AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> _assertionBuilder;

    public ValueAnd(AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> assertionBuilder)
    {
        _assertionBuilder = assertionBuilder;
    }
    
    public static ValueAnd<TActual> Create(AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> assertionBuilder)
    {
        return new ValueAnd<TActual>(assertionBuilder);
    }

    AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> ISource<TActual, ValueAnd<TActual>, ValueOr<TActual>>.AssertionBuilder => new AndAssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>(_assertionBuilder);
}