namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableNotEquivalentToExpectedValueAssertCondition<TActual, TInner>(
    IEnumerable<TInner> expected,
    IEqualityComparer<TInner?>? equalityComparer)
    : ExpectedValueAssertCondition<TActual, IEnumerable<TInner>>(expected)
    where TActual : IEnumerable<TInner>?
{
    protected override string GetFailureMessage(TActual? actualValue, IEnumerable<TInner>? expected) => $"""
         The two Enumerables were equivalent
            {string.Join(',', ActualValue ?? Enumerable.Empty<TInner>())}
         """;

    protected override bool Passes(TActual? actualValue, IEnumerable<TInner>? expectedValue)
    {
        if (actualValue is null && expectedValue is null)
        {
            return false;
        }
        
        if (actualValue is null || expectedValue is null)
        {
            return true;
        }
        
        return !actualValue.SequenceEqual(expectedValue!, equalityComparer);
    }
}