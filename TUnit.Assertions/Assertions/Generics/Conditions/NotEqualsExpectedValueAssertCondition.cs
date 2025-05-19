﻿using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class NotEqualsExpectedValueAssertCondition<TActual>(TActual expected)
    : ExpectedValueAssertCondition<TActual, TActual>(expected)
{
    internal protected override string GetExpectation()
        => $"to not be equal to {ExpectedValue}";

    protected override ValueTask<AssertionResult> GetResult(TActual? actualValue, TActual? expectedValue) => AssertionResult
        .FailIf(Equals(actualValue, expectedValue),
            "it was");
}