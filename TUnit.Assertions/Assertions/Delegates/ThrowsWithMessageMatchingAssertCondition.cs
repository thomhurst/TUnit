using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Delegates;

public class ThrowsWithMessageMatchingAssertCondition<TActual, TException>(
    string expectedMessage,
    StringComparison stringComparison,
    Func<Exception?, Exception?> exceptionSelector)
    : DelegateAssertCondition<TActual, Exception>
    where TException : Exception
{
    protected override string GetExpectation()
        => $"to throw {typeof(TException).Name.PrependAOrAn()} which message matches \"{expectedMessage.ShowNewLines().TruncateWithEllipsis(100)}\"";

    protected override AssertionResult GetResult(TActual? actualValue, Exception? exception)
    {
        var actualException = exceptionSelector(exception);

        return AssertionResult
            .FailIf(
                () => actualException is null,
                "the exception is null")
            .OrFailIf(
                () => !string.Equals(actualException!.Message, expectedMessage, stringComparison),
                $"found \"{actualException!.Message.ShowNewLines().TruncateWithEllipsis(100)}\"");
    }
}