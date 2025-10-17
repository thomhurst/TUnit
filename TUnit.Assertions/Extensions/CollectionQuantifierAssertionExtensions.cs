using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Conditions.Wrappers;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for collection quantifier assertions.
/// Includes: HasCount, All, Any, HasSingleItem, HasDistinctItems
/// </summary>
public static class CollectionQuantifierAssertionExtensions
{
    /// <summary>
    /// Returns a wrapper for collection count assertions.
    /// Example: await Assert.That(list).HasCount().EqualTo(5);
    /// </summary>
    public static CountWrapper<TCollection, TItem> HasCount<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append(".HasCount()");
        return new CountWrapper<TCollection, TItem>(source.Context);
    }

    /// <summary>
    /// Asserts that the collection has the expected count.
    /// Example: await Assert.That(list).HasCount(5);
    /// </summary>
    public static CollectionCountAssertion<TCollection, TItem> HasCount<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".HasCount({expression})");
        return new CollectionCountAssertion<TCollection, TItem>(source.Context, expectedCount);
    }

    /// <summary>
    /// Creates a helper for asserting that all items in the collection satisfy custom assertions.
    /// Example: await Assert.That(list).All().Satisfy(item => item.IsNotNull());
    /// </summary>
    public static CollectionAllSatisfyHelper<TCollection, TItem> All<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append(".All()");
        return new CollectionAllSatisfyHelper<TCollection, TItem>(source.Context);
    }

    /// <summary>
    /// Asserts that all items in the collection satisfy the predicate.
    /// </summary>
    public static CollectionAllAssertion<TCollection, TItem> All<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".All({expression})");
        return new CollectionAllAssertion<TCollection, TItem>(source.Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that at least one item in the collection satisfies the predicate.
    /// </summary>
    public static CollectionAnyAssertion<TCollection, TItem> Any<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".Any({expression})");
        return new CollectionAnyAssertion<TCollection, TItem>(source.Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that the collection contains exactly one item.
    /// </summary>
    public static HasSingleItemAssertion<TCollection, TItem> HasSingleItem<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append(".HasSingleItem()");
        return new HasSingleItemAssertion<TCollection, TItem>(source.Context);
    }

    /// <summary>
    /// Asserts that the collection contains only distinct (unique) items.
    /// </summary>
    public static HasDistinctItemsAssertion<TCollection> HasDistinctItems<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append(".HasDistinctItems()");
        return new HasDistinctItemsAssertion<TCollection>(source.Context);
    }
}
