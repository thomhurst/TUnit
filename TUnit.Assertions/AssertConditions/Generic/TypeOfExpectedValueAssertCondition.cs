namespace TUnit.Assertions.AssertConditions.Generic;

public class TypeOfExpectedValueAssertCondition<TActual>(Type expectedType)
    : BaseAssertCondition<TActual>
{
    protected override string GetExpectation()
        => $"to be of type {expectedType.Name}";

    protected override AssertionResult GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult
            .FailIf(
                () => actualValue?.GetType() != expectedType,
                $"{actualValue} it is {ActualValue?.GetType().Name ?? "null"}");
}