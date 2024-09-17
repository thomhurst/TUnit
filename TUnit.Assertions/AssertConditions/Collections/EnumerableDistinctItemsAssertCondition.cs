using System.Collections;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableDistinctItemsAssertCondition<TActual, TInner, TAnd, TOr> : AssertCondition<TActual, object, TAnd, TOr>
    where TActual : IEnumerable
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly IEqualityComparer<TInner?>? _equalityComparer;

    public EnumerableDistinctItemsAssertCondition(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TInner expected,
        IEqualityComparer<TInner?>? equalityComparer) : base(expected)
    {
        _equalityComparer = equalityComparer;
    }

    protected override string DefaultMessage => "Duplicate items found in the collection";

    protected internal override bool Passes(TActual? actualValue, Exception? exception, string? rawValueExpression)
    {
        if (actualValue is null)
        {
            WithMessage((_, _, actualExpression) => $"{actualExpression ?? typeof(TActual).Name} is null");
            return false;
        }

        var list = actualValue.Cast<TInner>().ToList();

        var distinct = list.Distinct(_equalityComparer);
        
        return list.Count == distinct.Count();
    }
}