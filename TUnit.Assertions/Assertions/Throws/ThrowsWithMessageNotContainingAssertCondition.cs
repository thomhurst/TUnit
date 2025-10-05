using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageNotContainingAssertCondition<TActual, TException>(
    string expected,
    StringComparison stringComparison,
    Func<Exception?, Exception?> exceptionSelector)
    : DelegateAssertCondition<TActual, Exception>
    where TException : Exception
{
    protected internal override string GetExpectation()
        => $"to throw {typeof(TException).Name.PrependAOrAn()} which message does not contain \"{expected?.ShowNewLines().TruncateWithEllipsis(100)}\"";

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        var actualException = exceptionSelector(exception);

        return AssertionResult
            .FailIf(actualException is null,
                "the exception is null")
            .OrFailIf(actualException is not null && actualException.Message.Contains(expected, stringComparison),
                $"found \"{actualException?.Message.ShowNewLines().TruncateWithEllipsis(100)}\"");
    }
}