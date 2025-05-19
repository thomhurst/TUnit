using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class TypeOfExpectedValueAssertCondition<TActual>(Type expectedType)
    : BaseAssertCondition<TActual>
{
    internal protected override string GetExpectation()
        => $"to be of type {expectedType.Name}";

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
        => AssertionResult
            .FailIf(actualValue?.GetType() != expectedType,
                $"{actualValue} it is {ActualValue?.GetType().Name ?? "null"}");
}