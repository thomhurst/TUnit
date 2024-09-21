using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueOr<TActual> : IValueSource<TActual>
{
    private readonly AssertionBuilder<TActual> _assertionBuilder;

    public ValueOr(AssertionBuilder<TActual> assertionBuilder)
    {
        _assertionBuilder = assertionBuilder;
    }
    
    public static ValueOr<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueOr<TActual>(assertionBuilder);
    }
    
    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => new OrAssertionBuilder<TActual>(_assertionBuilder);
}