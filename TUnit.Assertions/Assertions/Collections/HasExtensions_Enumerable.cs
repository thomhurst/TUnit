#nullable disable

using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static SingleItemAssertion<IEnumerable<TInner>, TInner> HasSingleItem<TInner>(this IValueSource<IEnumerable<TInner>> valueSource)
    {
        var invokableValueAssertionBuilder = valueSource.RegisterAssertion(new EnumerableCountEqualToExpectedValueAssertCondition<IEnumerable<TInner>, TInner>(1), []);

        return new SingleItemAssertion<IEnumerable<TInner>, TInner>(invokableValueAssertionBuilder);
    }

    public static AssertionBuilder<IEnumerable<TInner>> HasDistinctItems<TInner>(this IValueSource<IEnumerable<TInner>> valueSource)
    {
        return HasDistinctItems(valueSource, EqualityComparer<TInner>.Default);
    }

    public static AssertionBuilder<IEnumerable<TInner>> HasDistinctItems<TInner>(this IValueSource<IEnumerable<TInner>> valueSource, IEqualityComparer<TInner> equalityComparer)
    {
        return valueSource.RegisterAssertion(new EnumerableDistinctItemsExpectedValueAssertCondition<IEnumerable<TInner>, TInner>(equalityComparer), []);
    }

    public static EnumerableCount<IEnumerable<TInner>, TInner> HasCount<TInner>(this IValueSource<IEnumerable<TInner>> valueSource)
    {
        valueSource.AppendExpression("HasCount()");
        return new EnumerableCount<IEnumerable<TInner>, TInner>(valueSource);
    }

    public static AssertionBuilder<IEnumerable<TInner>> HasCount<TInner>(this IValueSource<IEnumerable<TInner>> valueSource, int count)
    {
        return HasCount(valueSource).EqualTo(count);
    }
}
