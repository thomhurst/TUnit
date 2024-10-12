using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageAssertCondition<TActual, TException>(
    string expectedMessage,
    StringComparison stringComparison,
    Func<Exception?, Exception?> exceptionSelector)
    : DelegateAssertCondition<TActual, Exception>
    where TException : Exception
{
    protected override string GetExpectation()
        => $"to throw {typeof(TException).Name.PrependAOrAn()} which message equals \"{expectedMessage.ShowNewLines().TruncateWithEllipsis(100)}\"";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
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