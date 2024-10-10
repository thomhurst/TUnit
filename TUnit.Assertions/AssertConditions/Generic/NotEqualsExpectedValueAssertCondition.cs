namespace TUnit.Assertions.AssertConditions.Generic;

public class NotEqualsExpectedValueAssertCondition<TActual>(TActual expected)
    : ExpectedValueAssertCondition<TActual, TActual>(expected)
{
    protected override string GetExpectation()
        => $"to not be equal to {expected}";

    protected internal override AssertionResult Passes(TActual? actualValue, TActual? expectedValue) => AssertionResult
        .FailIf(
            () => Equals(actualValue, expectedValue),
            "it was");
}