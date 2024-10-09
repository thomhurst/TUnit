namespace TUnit.Assertions.AssertConditions.Generic;

public class SameReferenceExpectedValueAssertCondition<TActual, TExpected>(TExpected expected)
    : ExpectedValueAssertCondition<TActual, TExpected>(expected)
{
    protected override string GetFailureMessage(TActual? actualValue, TExpected? expectedValue) => "The two objects are different references.";

    protected override AssertionResult Passes(TActual? actualValue, TExpected? expectedValue)
    {
        return ReferenceEquals(actualValue, ExpectedValue);
    }
}