namespace TUnit.Assertions.AssertConditions.Generic;

public class NotTypeOfExpectedValueAssertCondition<TActual, TExpected>
    : BaseAssertCondition<TActual>
{
    protected override string GetExpectation()
        => $"to not be of type {typeof(TExpected).Name}";

    protected override AssertionResult GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult
            .FailIf(
                () => actualValue?.GetType() == typeof(TExpected),
                "it was");
}