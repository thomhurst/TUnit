using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageContainingExpectedValueAssertCondition<TActual>(
    string expectedMessage,
    StringComparison stringComparison,
    Func<Exception?, Exception?> exceptionSelector)
    : DelegateAssertCondition<TActual, Exception>
{
	protected internal override string GetFailureMessage()
		=> $"to have Message containing \"{expectedMessage}\"";

	protected internal override AssertionResult Passes(TActual? actualValue, Exception? exception)
	{
		var actualException = exceptionSelector(exception);

		return AssertionResult
			.FailIf(
				() => actualException is null,
				"the exception is null")
			.OrFailIf(
				() => !string.Equals(exception.Message, expectedMessage, stringComparison),
				$"it was not found");
	}
}