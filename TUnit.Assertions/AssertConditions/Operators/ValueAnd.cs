using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueAnd<TActual>(AssertionBuilder<TActual> assertionBuilder) : IValueSource<TActual>
{
    public static ValueAnd<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueAnd<TActual>(assertionBuilder);
    }

    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => new AndAssertionBuilder<TActual>(assertionBuilder);
}