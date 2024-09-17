using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<TActual, TExpected, TAnd, TOr>(TExpected? expected) : BaseAssertCondition<TActual>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    internal TExpected? ExpectedValue { get; } = expected;
}