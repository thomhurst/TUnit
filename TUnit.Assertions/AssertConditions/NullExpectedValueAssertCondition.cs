namespace TUnit.Assertions.AssertConditions;

public class NullExpectedValueAssertCondition<TActual> : BaseAssertCondition<TActual>
{
	protected internal override string GetFailureMessage()
		=> "to be null";

	protected internal override AssertionResult Passes(TActual? actualValue, Exception? exception)
		=> AssertionResult
			.FailIf(
				() => actualValue is not null,
				$"found {actualValue}");
}