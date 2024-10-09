namespace TUnit.Assertions.AssertConditions;

public class NotNullExpectedValueAssertCondition<TActual> : BaseAssertCondition<TActual>
{
	protected internal override string GetFailureMessage()
		=> "to not be null";

	protected internal override AssertionResult Passes(TActual? actualValue, Exception? exception)
		=> AssertionResult
			.FailIf(
				() => actualValue is null,
				"it was");
}