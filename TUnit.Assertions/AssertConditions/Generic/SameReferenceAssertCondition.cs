namespace TUnit.Assertions.AssertConditions.Generic;

public class SameReferenceAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{

    public SameReferenceAssertCondition(TExpected expected) : base(expected)
    {
    }

    protected internal override string GetFailureMessage() => "The two objects are different references.";

    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return ReferenceEquals(actualValue, ExpectedValue);
    }
}