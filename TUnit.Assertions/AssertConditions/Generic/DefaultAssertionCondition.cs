namespace TUnit.Assertions.AssertConditions.Generic;

public class DefaultExpectedValueAssertCondition<TActual> : BaseAssertCondition<TActual>
{
    private readonly TActual? _defaultValue = default;

    protected override string GetExpectation()
        => $"to be {(_defaultValue is null ? "null" : _defaultValue)}";

    protected override AssertionResult GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult
            .FailIf(
                () => actualValue is not null && !actualValue.Equals(_defaultValue),
                $"found {actualValue}");
}