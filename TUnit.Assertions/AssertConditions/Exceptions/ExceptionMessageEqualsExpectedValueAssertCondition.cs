using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.Exceptions;

public class ExceptionMessageEqualsExpectedValueAssertCondition<TException>(string expected, StringComparison stringComparison)
    : ExpectedValueAssertCondition<TException, string>(expected)
where TException : Exception
{
    protected override string GetExpectation()
        => $"message to be equal to {Formatter.Format(expected).TruncateWithEllipsis(100)}";

    protected override AssertionResult GetResult(TException? actualValue, string? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(
                    () => expectedValue is not null,
                    "the exception was null");
        }

        return AssertionResult
            .FailIf(
                () => !string.Equals(actualValue.Message, expectedValue, stringComparison),
                $"found message {Formatter.Format(actualValue.Message).TruncateWithEllipsis(100)} which {new StringDifference(actualValue.Message, expectedValue)}");
    }
}