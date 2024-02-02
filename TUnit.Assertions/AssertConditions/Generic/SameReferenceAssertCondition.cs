namespace TUnit.Assertions.AssertConditions.Generic;

public class SameReferenceAssertCondition<TActual, TExpected> : AssertCondition<TActual, TExpected>
{

    public SameReferenceAssertCondition(AssertionBuilder<TActual> assertionBuilder, TExpected expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => "The two objects are different references.";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        return ReferenceEquals(actualValue, ExpectedValue);
    }
}