using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.Exceptions;

public class ExceptionMessageMatchingExpectedAssertCondition<TException>(StringMatcher match)
    : ExpectedValueAssertCondition<TException, StringMatcher>(match)
where TException : Exception
{
    internal protected override string GetExpectation()
        => $"message to match {Formatter.Format(ExpectedValue).TruncateWithEllipsis(100)}";

    protected override ValueTask<AssertionResult> GetResult(TException? actualValue, StringMatcher? expectedValue)
    {
        if (actualValue?.Message is null)
        {
            return AssertionResult
                .FailIf(expectedValue is not null,
                    "the exception message was null");
        }

        return AssertionResult
            .FailIf(expectedValue is null,
                "expected value was null")
            .OrFailIf(!ExpectedValue!.Matches(actualValue.Message),
                $"found message {Formatter.Format(actualValue.Message).TruncateWithEllipsis(100)}");
    }
}