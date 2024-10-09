﻿namespace TUnit.Assertions.AssertConditions.Generic;

public class EqualsExpectedValueAssertCondition<TActual>(TActual expected) : ExpectedValueAssertCondition<TActual, TActual>(expected)
{
	protected internal override string GetFailureMessage()
		=> $"to be equal to {expected}";

    protected internal override AssertionResult Passes(TActual? actualValue, TActual? expectedValue)
    {
        if (actualValue is IEquatable<TActual> equatable)
		{
			return AssertionResult
				.FailIf(
					() => !equatable.Equals(expected),
					$"found {actualValue}");
        }

		return AssertionResult
			.FailIf(
				() => !Equals(actualValue, ExpectedValue),
				$"found {actualValue}");
    }
}