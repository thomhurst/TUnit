using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageEqualToExpectedValueAssertCondition<TActual>(
    string expectedMessage,
    StringComparison stringComparison,
    Func<Exception?, Exception?> exceptionSelector)
    : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetExpectation()
        => $"to have Message equal to \"{expectedMessage.ShowNewLines().TruncateWithEllipsis(100)}\"";

    protected override AssertionResult GetResult(TActual? actualValue, Exception? exception)
    {
        var actualException = exceptionSelector(exception);

        return AssertionResult
            .FailIf(
                () => actualException is null,
                "the exception is null")
            .OrFailIf(
                () => !string.Equals(actualException!.Message, expectedMessage, stringComparison),
                new StringDifference(actualException!.Message, expectedMessage)
                    .ToString("it differs at index"));
    }
}