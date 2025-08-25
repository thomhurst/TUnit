#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#endif
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class GenericEqualsExpectedValueAssertCondition<TActual, TExpected>(TExpected expected) : ExpectedValueAssertCondition<TActual, TExpected>(expected)
    where TActual : IEquatable<TExpected>
{
    protected internal override string GetExpectation()
        => $"to be equal to {ExpectedValue}";

    protected override ValueTask<AssertionResult> GetResult(TActual? actualValue, TExpected? expectedValue)
    {
        return AssertionResult
            .FailIf(!(actualValue?.Equals(expectedValue) ?? expectedValue == null),
                $"found {actualValue}");
    }
}
