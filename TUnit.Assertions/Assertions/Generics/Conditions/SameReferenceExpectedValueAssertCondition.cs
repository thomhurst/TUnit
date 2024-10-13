using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class SameReferenceExpectedValueAssertCondition<TActual, TExpected>(TExpected expected)
    : ExpectedValueAssertCondition<TActual, TExpected>(expected)
{
    protected override string GetExpectation()
        => $"to have the same reference as {expected}";

    protected override AssertionResult GetResult(TActual? actualValue, TExpected? expectedValue) => AssertionResult
        .FailIf(
            () => !ReferenceEquals(actualValue, expectedValue),
            "they did not");
}