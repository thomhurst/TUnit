namespace TUnit.Assertions.AssertConditions.Generic;

public class NotDefaultExpectedValueAssertCondition<TActual>() : ExpectedValueAssertCondition<TActual, TActual>(default)
{
		private readonly TActual? _defaultValue = default;

		protected internal override string GetFailureMessage()
			=> $"to not be {(_defaultValue is null ? "null" : _defaultValue)}";

		protected internal override AssertionResult Passes(TActual? actualValue, TActual? expectedValue)
			=> AssertionResult
				.FailIf(
					() => actualValue is null || actualValue.Equals(_defaultValue),
					"it was");
}