using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class NotAssignableToExpectedValueAssertCondition<TActual>(Type expectedType)
    : BaseAssertCondition<TActual>
{
    protected override string GetExpectation()
        => $"to not be assignable to type {expectedType.Name}";

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
        => AssertionResult
            .FailIf(actualValue is null,
                "actual is null")
            .OrFailIf(expectedType.IsInstanceOfType(actualValue!),
                $"it is {ActualValue?.GetType().Name ?? "null"}");
}