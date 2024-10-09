namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableEquivalentToExpectedValueAssertCondition<TActual, TInner>(
    IEnumerable<TInner> expected,
    IEqualityComparer<TInner?>? equalityComparer)
    : ExpectedValueAssertCondition<TActual, IEnumerable<TInner>>(expected)
    where TActual : IEnumerable<TInner>?
{
    protected internal override string GetFailureMessage() => $" to be equivalent to {(expected != null ? string.Join(',', expected) : null)}";

    protected override AssertionResult Passes(TActual? actualValue, IEnumerable<TInner>? expectedValue)
    {
        if (actualValue is null && expectedValue is null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult
            .FailIf(
                () => actualValue is null,
                "it is null")
            .OrFailIf(
                () => expectedValue is null,
                "it is not null")
            .OrFailIf(
                () => !actualValue.SequenceEqual(expectedValue, equalityComparer),
                $"it is {(actualValue != null ? string.Join(',', actualValue) : null)}"
            );
    }
}