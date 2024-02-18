using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions;

public abstract class AssertCondition<TActual, TExpected, TAnd, TOr> : BaseAssertCondition<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    internal TExpected? ExpectedValue { get; }
    
    internal AssertCondition(AssertionBuilder<TActual> assertionBuilder, TExpected? expected) : base(assertionBuilder)
    {
        ExpectedValue = expected;
    }
}