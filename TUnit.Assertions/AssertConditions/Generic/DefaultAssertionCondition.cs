namespace TUnit.Assertions.AssertConditions.Generic;

public class DefaultExpectedValueAssertCondition<TActual> : BaseAssertCondition<TActual>
{
	private readonly TActual? _defaultValue = default;

	protected internal override string GetFailureMessage()
		=> $"to be {(_defaultValue is null ? "null" : _defaultValue)}";

	protected internal override AssertionResult Passes(TActual? actualValue, Exception? exception)
		=> AssertionResult
			.FailIf(
				() => actualValue is not null && !actualValue.Equals(_defaultValue),
				$"found {actualValue}");
}