namespace TUnit.Assertions.AssertConditions.Generic;

public class SameReferenceAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{

    public SameReferenceAssertCondition(IReadOnlyCollection<AssertCondition<TActual, TExpected>> previousConditions, TExpected expected) : base(previousConditions, expected)
    {
    }

    public override string DefaultMessage => "The two objects are different references.";

    protected override bool Passes(TActual actualValue)
    {
        return ReferenceEquals(actualValue, ExpectedValue);
    }
}