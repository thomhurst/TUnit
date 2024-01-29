namespace TUnit.Assertions.AssertConditions.Generic;

public class SameReferenceAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{

    public SameReferenceAssertCondition(TExpected expected) : base(expected)
    {
    }

    public override string DefaultMessage => "The two objects are different references.";

    protected internal override bool Passes(TActual actualValue)
    {
        return ReferenceEquals(actualValue, ExpectedValue);
    }
}