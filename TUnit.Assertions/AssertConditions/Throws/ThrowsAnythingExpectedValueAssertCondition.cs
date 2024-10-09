namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsAnythingExpectedValueAssertCondition<TActual>
    : DelegateAssertCondition<TActual, Exception>
{
	protected internal override string GetFailureMessage()
		=> "to throw an exception";

	protected internal override AssertionResult Passes(TActual? actualValue, Exception? exception)
		=> AssertionResult.FailIf(
			() => exception is null,
			$"none was thrown");
}