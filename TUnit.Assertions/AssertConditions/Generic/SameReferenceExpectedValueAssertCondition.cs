namespace TUnit.Assertions.AssertConditions.Generic;

public class SameReferenceExpectedValueAssertCondition<TActual, TExpected>(TExpected expected)
    : ExpectedValueAssertCondition<TActual, TExpected>(expected)
{
	protected override string GetExpectation()
		=> $"to have the same reference as {expected}";

	protected internal override AssertionResult Passes(TActual? actualValue, TExpected? expectedValue) => AssertionResult
		.FailIf(
			() => !ReferenceEquals(actualValue, expectedValue),
			"they did not");
}