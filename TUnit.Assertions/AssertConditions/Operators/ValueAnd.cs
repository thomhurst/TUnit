using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueAnd<TActual>(AssertionBuilder<TActual> assertionBuilder) : ValueSource<TActual>(assertionBuilder)
{
    public static ValueAnd<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueAnd<TActual>(assertionBuilder);
    }
}