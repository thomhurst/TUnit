using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Wrappers;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class NotSameReferenceExpectedValueAssertCondition<TActual, TExpected>(TExpected expected)
    : ExpectedValueAssertCondition<TActual, TExpected>(expected)
{
    internal protected override string GetExpectation()
        => $"to not have the same reference as {ExpectedValue}";

    protected override ValueTask<AssertionResult> GetResult(TActual? actualValue, TExpected? expectedValue)
    {
        if (actualValue is UnTypedEnumerableWrapper unTypedEnumerableWrapper)
        {
            return AssertionResult
                .FailIf(ReferenceEquals(unTypedEnumerableWrapper.Enumerable, expectedValue),
                    "they did");
        }
        
        return AssertionResult
            .FailIf(ReferenceEquals(actualValue, expectedValue),
                "they did");
    }
}