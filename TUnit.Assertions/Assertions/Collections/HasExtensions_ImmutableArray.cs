#nullable disable

using System.Collections.Immutable;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static SingleItemAssertionBuilderWrapper<ImmutableArray<TInner>, TInner> HasSingleItem<TInner>(this IValueSource<ImmutableArray<TInner>> valueSource)
    {
        var invokableValueAssertionBuilder = valueSource.RegisterAssertion(new EnumerableCountEqualToExpectedValueAssertCondition<ImmutableArray<TInner>, TInner>(1), []);

        return new SingleItemAssertionBuilderWrapper<ImmutableArray<TInner>, TInner>(invokableValueAssertionBuilder);
    }

    public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> HasDistinctItems<TInner>(this IValueSource<ImmutableArray<TInner>> valueSource)
    {
        return HasDistinctItems(valueSource, EqualityComparer<TInner>.Default);
    }

    public static InvokableValueAssertionBuilder<ImmutableArray<TInner>> HasDistinctItems<TInner>(this IValueSource<ImmutableArray<TInner>> valueSource, IEqualityComparer<TInner> equalityComparer)
    {
        return valueSource.RegisterAssertion(new EnumerableDistinctItemsExpectedValueAssertCondition<ImmutableArray<TInner>, TInner>(equalityComparer), []);
    }

    public static EnumerableCount<ImmutableArray<TInner>, TInner> HasCount<TInner>(this IValueSource<ImmutableArray<TInner>> valueSource)
    {
        valueSource.AppendExpression("HasCount()");
        return new EnumerableCount<ImmutableArray<TInner>, TInner>(valueSource);
    }
}
