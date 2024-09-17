namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableEquivalentToAssertCondition<TActual, TInner> : AssertCondition<TActual, IEnumerable<TInner>>
    where TActual : IEnumerable<TInner>?
{
    private readonly IEqualityComparer<TInner?>? _equalityComparer;

    public EnumerableEquivalentToAssertCondition(IEnumerable<TInner> expected, IEqualityComparer<TInner?>? equalityComparer) : base(expected)
    {
        _equalityComparer = equalityComparer;
    }

    protected internal override string GetFailureMessage() => $"""
                                                The two Enumerables were not equivalent
                                                   Actual: {(ActualValue != null ? string.Join(',', ActualValue) : null)}
                                                   Expected: {(ExpectedValue != null ? string.Join(',', ExpectedValue) : null)}
                                                """;

    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return true;
        }

        if (actualValue is null || ExpectedValue is null)
        {
            return false;
        }
        
        return actualValue?.SequenceEqual(ExpectedValue, _equalityComparer) ?? false;
    }
}