using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class AssignableFromExpectedValueAssertCondition<TActual>(Type expectedType)
    : BaseAssertCondition<TActual>
{
    protected override string GetExpectation()
        => $"to be assignable from type {expectedType.Name}";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult
            .FailIf(() => actualValue is null,
                "actual is null")
            .OrFailIf(
                () => !actualValue!.GetType().IsAssignableFrom(expectedType),
                $"it is {ActualValue?.GetType().Name ?? "null"}");
}