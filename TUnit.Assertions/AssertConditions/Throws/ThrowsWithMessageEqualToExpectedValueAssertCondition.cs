namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageEqualToExpectedValueAssertCondition<TActual>(
    string expectedMessage,
    StringComparison stringComparison,
    Func<Exception?, Exception?> exceptionSelector)
    : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetFailureMessage(Exception? exception) => $"Message was {exceptionSelector(exception)?.Message} instead of {expectedMessage}";

    protected override bool Passes(Exception? rootException)
    {
        var exception = exceptionSelector(rootException);

        if (exception is null)
        {
            return FailWithMessage("Exception is null");
        }
        
        return string.Equals(exception.Message, expectedMessage, stringComparison);
    }
}