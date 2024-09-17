using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueOr<TActual> 
    : Or<TActual, ValueAnd<TActual>, ValueOr<TActual>>, IOr<TActual, ValueAnd<TActual>, ValueOr<TActual>>,
        IValueSource<TActual, ValueAnd<TActual>, ValueOr<TActual>>
{
    private readonly AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> _assertionBuilder;

    public ValueOr(AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> assertionBuilder)
    {
        _assertionBuilder = assertionBuilder;
    }
    
    public static ValueOr<TActual> Create(AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> assertionBuilder)
    {
        return new ValueOr<TActual>(assertionBuilder);
    }
    
    AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> ISource<TActual, ValueAnd<TActual>, ValueOr<TActual>>.AssertionBuilder => new OrAssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>(_assertionBuilder);
}