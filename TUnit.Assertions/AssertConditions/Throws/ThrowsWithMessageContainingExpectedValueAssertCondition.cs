namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageContainingExpectedValueAssertCondition<TActual>(
    string expectedMessage,
    StringComparison stringComparison,
    Func<Exception?, Exception?> exceptionSelector)
    : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetExpectation()
        => $"to have Message containing \"{expectedMessage}\"";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
    {
        var actualException = exceptionSelector(exception);

        return AssertionResult
            .FailIf(
                () => actualException is null,
                "the exception is null")
            .OrFailIf(
                () => !actualException!.Message.Contains(expectedMessage, stringComparison),
                "it was not found");
    }
}