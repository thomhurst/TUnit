namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableNotEquivalentToExpectedValueAssertCondition<TActual, TInner>(
    IEnumerable<TInner> expected,
    IEqualityComparer<TInner?>? equalityComparer)
    : ExpectedValueAssertCondition<TActual, IEnumerable<TInner>>(expected)
    where TActual : IEnumerable<TInner>?
{
    protected override string GetExpectation() => $" to be not equivalent to {(expected != null ? string.Join(',', expected) : null)}";

    protected override AssertionResult GetResult(TActual? actualValue, IEnumerable<TInner>? expectedValue)
    {
        if (actualValue is null != expectedValue is null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult
            .FailIf(
                () => actualValue is null && expectedValue is null,
                "it is null")
            .OrFailIf(
                () => actualValue!.SequenceEqual(expectedValue!, equalityComparer),
                "the two Enumerables were equivalent"
            );
    }
}