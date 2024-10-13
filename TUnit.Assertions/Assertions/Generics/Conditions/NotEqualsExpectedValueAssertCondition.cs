namespace TUnit.Assertions.AssertConditions.Generic;

public class NotEqualsExpectedValueAssertCondition<TActual>(TActual expected)
    : ExpectedValueAssertCondition<TActual, TActual>(expected)
{
    protected override string GetExpectation()
        => $"to not be equal to {expected}";

    protected override AssertionResult GetResult(TActual? actualValue, TActual? expectedValue) => AssertionResult
        .FailIf(
            () => Equals(actualValue, expectedValue),
            "it was");
}