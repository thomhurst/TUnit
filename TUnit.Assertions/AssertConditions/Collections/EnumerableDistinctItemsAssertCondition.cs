using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableDistinctItemsAssertCondition<TActual, TInner> : AssertCondition<TActual, object>
    where TActual : IEnumerable
{
    private readonly IEqualityComparer<TInner?>? _equalityComparer;

    public EnumerableDistinctItemsAssertCondition(TInner expected,
        IEqualityComparer<TInner?>? equalityComparer) : base(expected)
    {
        _equalityComparer = equalityComparer;
    }

    protected internal override string GetFailureMessage() => "Duplicate items found in the collection";

    private protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            OverriddenMessage = $"{ActualExpression ?? typeof(TActual).Name} is null";
            return false;
        }

        var list = actualValue.Cast<TInner>().ToList();

        var distinct = list.Distinct(_equalityComparer);
        
        return list.Count == distinct.Count();
    }
}