using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableNotEquivalentToAssertCondition<TActual, TInner, TAnd, TOr> : AssertCondition<TActual, IEnumerable<TInner>, TAnd, TOr>
    where TActual : IEnumerable<TInner>?
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly IEqualityComparer<TInner?>? _equalityComparer;

    public EnumerableNotEquivalentToAssertCondition(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, IEnumerable<TInner> expected, IEqualityComparer<TInner?>? equalityComparer) : base(expected)
    {
        _equalityComparer = equalityComparer;
    }

    protected override string DefaultMessage => $"""
                                                The two Enumerables were equivalent
                                                   {string.Join(',', ActualValue ?? Enumerable.Empty<TInner>())}
                                                """;

    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
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