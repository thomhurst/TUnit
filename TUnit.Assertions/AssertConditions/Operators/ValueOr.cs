using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueOr<TActual>(AssertionBuilder<TActual> assertionBuilder) : IValueSource<TActual>
{
    public static ValueOr<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueOr<TActual>(assertionBuilder);
    }
    
    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => new OrAssertionBuilder<TActual>(assertionBuilder);
}