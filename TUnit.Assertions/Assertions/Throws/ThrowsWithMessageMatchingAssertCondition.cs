using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithMessageMatchingAssertCondition<TActual, TException>(
    StringMatcher match,
    Func<Exception?, Exception?> exceptionSelector)
    : DelegateAssertCondition<TActual, Exception>
    where TException : Exception
{
    internal protected override string GetExpectation()
        => $"to throw {typeof(TException).Name.PrependAOrAn()} which message matches \"{match.ToString()?.ShowNewLines().TruncateWithEllipsis(100)}\"";

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        var actualException = exceptionSelector(exception);

        return AssertionResult
            .FailIf(actualException is null,
                "the exception is null")
            .OrFailIf(actualException is not null && !match.Matches(actualException.Message),
                $"found \"{actualException?.Message.ShowNewLines().TruncateWithEllipsis(100)}\"");
    }
}
