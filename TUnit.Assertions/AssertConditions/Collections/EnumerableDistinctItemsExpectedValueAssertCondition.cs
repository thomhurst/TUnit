using System.Collections;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableDistinctItemsExpectedValueAssertCondition<TActual, TInner>(IEqualityComparer<TInner?>? equalityComparer)
    : BaseAssertCondition<TActual>
    where TActual : IEnumerable
{
    protected internal override string GetFailureMessage() => "Duplicate items found in the collection";

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            return FailWithMessage($"{ActualExpression ?? typeof(TActual).Name} is null");
        }

        var list = actualValue.Cast<TInner>().ToList();

        var distinct = list.Distinct(equalityComparer);
        
        return list.Count == distinct.Count();
    }
}