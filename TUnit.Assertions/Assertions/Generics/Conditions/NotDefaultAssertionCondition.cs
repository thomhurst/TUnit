using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class NotDefaultExpectedValueAssertCondition<TActual>() : ExpectedValueAssertCondition<TActual, TActual>(default)
{
        private readonly TActual? _defaultValue = default;

        protected override string GetExpectation()
            => $"to not be {(_defaultValue is null ? "null" : _defaultValue)}";

        protected override AssertionResult GetResult(TActual? actualValue, TActual? expectedValue)
            => AssertionResult
                .FailIf(
                    () => actualValue is null || actualValue.Equals(_defaultValue),
                    "it was");
}