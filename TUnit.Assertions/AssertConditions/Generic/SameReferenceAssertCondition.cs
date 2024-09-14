using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Generic;

public class SameReferenceAssertCondition<TActual, TExpected, TAnd, TOr> : AssertCondition<TActual, TExpected, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{

    public SameReferenceAssertCondition(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TExpected expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => "The two objects are different references.";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return ReferenceEquals(actualValue, ExpectedValue);
    }
}