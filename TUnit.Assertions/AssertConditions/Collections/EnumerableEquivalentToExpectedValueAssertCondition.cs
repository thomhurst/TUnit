namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableEquivalentToExpectedValueAssertCondition<TActual, TInner>(
    IEnumerable<TInner> expected,
    IEqualityComparer<TInner?>? equalityComparer)
    : ExpectedValueAssertCondition<TActual, IEnumerable<TInner>>(expected)
    where TActual : IEnumerable<TInner>?
{
    protected override string GetFailureMessage(TActual? actualValue, IEnumerable<TInner>? expectedValue) => $"""
                                                                                                              The two Enumerables were not equivalent
                                                                                                                 Actual: {(actualValue != null ? string.Join(',', actualValue) : null)}
                                                                                                                 Expected: {(expectedValue != null ? string.Join(',', expectedValue) : null)}
                                                                                                              """;

    protected override bool Passes(TActual? actualValue, IEnumerable<TInner>? expectedValue)
    {
        if (actualValue is null && expectedValue is null)
        {
            return true;
        }

        if (actualValue is null || expectedValue is null)
        {
            return false;
        }
        
        return actualValue.SequenceEqual(expectedValue, equalityComparer);
    }
}