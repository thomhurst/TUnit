using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableContainsAssertCondition<TActual, TInner, TAnd, TOr> : AssertCondition<TActual, TInner, TAnd, TOr>
    where TActual : IEnumerable<TInner>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly IEqualityComparer<TInner?>? _equalityComparer;

    public EnumerableContainsAssertCondition(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TInner expected, IEqualityComparer<TInner?>? equalityComparer) : base(expected)
    {
        _equalityComparer = equalityComparer;
    }

    protected override string DefaultMessage => $"{ExpectedValue} was not found in the collection";

    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        if (actualValue is null)
        {
            WithMessage((_, _, actualExpression) => $"{actualExpression ?? typeof(TActual).Name} is null");
            return false;
        }
        
        return actualValue.Contains(ExpectedValue, _equalityComparer);
    }
}