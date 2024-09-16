using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<TActual, TExpected, TAnd, TOr> : BaseAssertCondition<TActual>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    internal TExpected? ExpectedValue { get; }
    
    internal AssertCondition(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TExpected? expected) : base(assertionBuilder)
    {
        ExpectedValue = expected;
    }
}