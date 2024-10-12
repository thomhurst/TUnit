using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Exceptions;

public class ExceptionMessageMatchingExpectedAssertCondition<TException>(StringMatcher match)
    : ExpectedValueAssertCondition<TException, StringMatcher>(match)
where TException : Exception
{
    protected override string GetExpectation()
        => $"message to match {Format(match).TruncateWithEllipsis(100)}";

    protected override AssertionResult GetResult(TException? actualValue, StringMatcher? expectedValue)
    {
        if (actualValue?.Message is null)
        {
            return AssertionResult
                .FailIf(
                    () => expectedValue is not null,
                    "the exception message was null");
        }

        return AssertionResult
            .FailIf(() => expectedValue is null,
                "expected value was null")
            .OrFailIf(
                () => !match.Matches(actualValue.Message),
                $"found {Format(actualValue).TruncateWithEllipsis(100)}");
    }
}