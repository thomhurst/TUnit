using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueOr<TActual>(AssertionBuilder<TActual> assertionBuilder) : ValueSource<TActual>(assertionBuilder)
{
}