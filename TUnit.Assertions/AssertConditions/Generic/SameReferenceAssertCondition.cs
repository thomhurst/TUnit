using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Generic;

public class SameReferenceAssertCondition<TActual, TExpected, TAnd, TOr> : AssertCondition<TActual?, TExpected, TAnd, TOr>
    where TAnd : And<TActual?, TAnd, TOr>, IAnd<TAnd, TActual?, TAnd, TOr>
    where TOr : Or<TActual?, TAnd, TOr>, IOr<TOr, TActual?, TAnd, TOr>
{

    public SameReferenceAssertCondition(AssertionBuilder<TActual?> assertionBuilder, TExpected expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => "The two objects are different references.";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return ReferenceEquals(actualValue, ExpectedValue);
    }
}