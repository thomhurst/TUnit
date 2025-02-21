using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class NotTypeOfExpectedValueAssertCondition<TActual>(Type expected)
    : BaseAssertCondition<TActual>
{
    protected override string GetExpectation()
        => $"to not be of type {expected.Name}";

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
        => AssertionResult
            .FailIf(actualValue?.GetType() == expected,
                "it was");
}