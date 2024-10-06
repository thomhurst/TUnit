namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageContainingExpectedValueAssertCondition<TActual>(
    string expectedMessage,
    StringComparison stringComparison,
    Func<Exception?, Exception?> exceptionSelector)
    : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetFailureMessage(Exception? exception) => $"Message '{exceptionSelector(Exception)?.Message}' did not contain '{expectedMessage}'";

    protected override bool Passes(Exception? rootException)
    {
        var exception = exceptionSelector(rootException);
        
        if (exception is null)
        {
            return FailWithMessage("Exception is null");
        }
        
        return exception.Message.Contains(expectedMessage, stringComparison);
    }
}