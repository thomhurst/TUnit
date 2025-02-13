using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsWithParamNameAssertCondition<TActual, TException>(
    string expectedParamName,
    StringComparison stringComparison,
    Func<Exception?, ArgumentException?> exceptionSelector)
    : DelegateAssertCondition<TActual, ArgumentException>()
    where TException : ArgumentException
{
    protected override string GetExpectation()
        => $"to throw {typeof(TException).Name.PrependAOrAn()} which param name equals \"{expectedParamName.TruncateWithEllipsis(100)}\"";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata)
    {
        var actualException = exceptionSelector(exception);

        return AssertionResult
            .FailIf(actualException is null,
                "the exception is null")
            .OrFailIf(!string.Equals(actualException!.ParamName, expectedParamName, stringComparison),
                $"{new StringDifference(actualException!.ParamName, expectedParamName)
                    .ToString("it differs at index")}");
    }
}
