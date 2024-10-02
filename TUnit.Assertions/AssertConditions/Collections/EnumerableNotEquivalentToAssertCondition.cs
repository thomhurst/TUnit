namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableNotEquivalentToAssertCondition<TActual, TInner> : AssertCondition<TActual, IEnumerable<TInner>>
    where TActual : IEnumerable<TInner>?
{
    private readonly IEqualityComparer<TInner?>? _equalityComparer;

    public EnumerableNotEquivalentToAssertCondition(IEnumerable<TInner> expected, IEqualityComparer<TInner?>? equalityComparer) : base(expected)
    {
        _equalityComparer = equalityComparer;
    }

    protected internal override string GetFailureMessage() => $"""
                                                The two Enumerables were equivalent
                                                   {string.Join(',', ActualValue ?? Enumerable.Empty<TInner>())}
                                                """;

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null && ExpectedValue is null)
        {
            return false;
        }
        
        if (actualValue is null || ExpectedValue is null)
        {
            return true;
        }
        
        return !actualValue.SequenceEqual(ExpectedValue!, _equalityComparer);
    }
}