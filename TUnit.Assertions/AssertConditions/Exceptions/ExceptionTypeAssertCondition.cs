using System;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertConditions.Exceptions;

public class ExceptionTypeAssertCondition : ExpectedValueAssertCondition<Exception, Type>
{
    public ExceptionTypeAssertCondition(Type expectedType)
        : base(expectedType)
    {
    }

    internal protected override string GetExpectation()
        => $"to be of type {ExpectedValue?.Name ?? "null"}";

    protected override ValueTask<AssertionResult> GetResult(Exception? actualValue, Type? expectedValue)
    {
        if (actualValue is null)
        {
            return AssertionResult
                .FailIf(true, "the exception was null");
        }

        if (expectedValue is null)
        {
            return AssertionResult
                .FailIf(true, "the expected type was null");
        }

        var actualType = actualValue.GetType();
        var isAssignable = expectedValue.IsAssignableFrom(actualType);

        return AssertionResult
            .FailIf(!isAssignable,
                $"found exception of type {actualType.Name}");
    }
}