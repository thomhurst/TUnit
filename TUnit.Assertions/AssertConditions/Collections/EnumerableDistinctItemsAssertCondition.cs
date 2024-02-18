using System.Collections;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableDistinctItemsAssertCondition<TActual, TInner, TAnd, TOr> : AssertCondition<TActual, object, TAnd, TOr>
    where TActual : IEnumerable
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    private readonly IEqualityComparer<TInner>? _equalityComparer;

    public EnumerableDistinctItemsAssertCondition(AssertionBuilder<TActual> assertionBuilder, TInner expected,
        IEqualityComparer<TInner>? equalityComparer) : base(assertionBuilder, expected)
    {
        _equalityComparer = equalityComparer;
    }

    protected override string DefaultMessage => "Duplicate items found in the collection";

    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
            return false;
        }

        var list = actualValue.Cast<TInner>().ToList();

        var distinct = _equalityComparer == null
            ? list.Distinct()
            : list.Distinct(_equalityComparer);
        
        return list.Count == distinct.Count();
    }
}