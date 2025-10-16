using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Conditions.Wrappers;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for collection assertions that target ICollectionAssertionSource.
/// These methods work on any class that implements ICollectionAssertionSource&lt;TCollection, TItem&gt;,
/// including collection assertions and their And/Or continuations.
/// </summary>
public static class CollectionAssertionExtensions
{
    /// <summary>
    /// Asserts that the collection is empty.
    /// </summary>
    public static CollectionIsEmptyAssertion<TCollection> IsEmpty<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append(".IsEmpty()");
        return new CollectionIsEmptyAssertion<TCollection>(source.Context);
    }

    /// <summary>
    /// Asserts that the collection is NOT empty.
    /// </summary>
    public static CollectionIsNotEmptyAssertion<TCollection> IsNotEmpty<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return new CollectionIsNotEmptyAssertion<TCollection>(source.Context);
    }

    /// <summary>
    /// Asserts that the collection contains the expected item.
    /// </summary>
    public static CollectionContainsAssertion<TCollection, TItem> Contains<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".Contains({expression})");
        return new CollectionContainsAssertion<TCollection, TItem>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the collection contains an item matching the predicate.
    /// </summary>
    public static CollectionContainsPredicateAssertion<TCollection, TItem> Contains<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".Contains({expression})");
        return new CollectionContainsPredicateAssertion<TCollection, TItem>(source.Context, predicate);
    }

    /// <summary>
    /// Asserts that the collection does NOT contain the expected item.
    /// </summary>
    public static CollectionDoesNotContainAssertion<TCollection, TItem> DoesNotContain<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".DoesNotContain({expression})");
        return new CollectionDoesNotContainAssertion<TCollection, TItem>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the collection does NOT contain any item matching the predicate.
    /// </summary>
    public static CollectionDoesNotContainPredicateAssertion<TCollection, TItem> DoesNotContain<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".DoesNotContain({expression})");
        return new CollectionDoesNotContainPredicateAssertion<TCollection, TItem>(source.Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that the collection contains ONLY items matching the predicate (all items satisfy the predicate).
    /// </summary>
    public static CollectionAllAssertion<TCollection, TItem> ContainsOnly<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".ContainsOnly({expression})");
        return new CollectionAllAssertion<TCollection, TItem>(source.Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in ascending order.
    /// </summary>
    public static CollectionIsOrderedByAssertion<TCollection, TItem, TKey> IsOrderedBy<TCollection, TItem, TKey>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        Func<TItem, TKey> keySelector,
        [CallerArgumentExpression(nameof(keySelector))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".IsOrderedBy({expression})");
        return new CollectionIsOrderedByAssertion<TCollection, TItem, TKey>(source.Context, keySelector);
    }

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in ascending order using the specified comparer.
    /// </summary>
    public static CollectionIsOrderedByAssertion<TCollection, TItem, TKey> IsOrderedBy<TCollection, TItem, TKey>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        Func<TItem, TKey> keySelector,
        IComparer<TKey> comparer,
        [CallerArgumentExpression(nameof(keySelector))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".IsOrderedBy({expression}, comparer)");
        return new CollectionIsOrderedByAssertion<TCollection, TItem, TKey>(source.Context, keySelector, comparer);
    }

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in descending order.
    /// </summary>
    public static CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey> IsOrderedByDescending<TCollection, TItem, TKey>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        Func<TItem, TKey> keySelector,
        [CallerArgumentExpression(nameof(keySelector))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".IsOrderedByDescending({expression})");
        return new CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey>(source.Context, keySelector);
    }

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in descending order using the specified comparer.
    /// </summary>
    public static CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey> IsOrderedByDescending<TCollection, TItem, TKey>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        Func<TItem, TKey> keySelector,
        IComparer<TKey> comparer,
        [CallerArgumentExpression(nameof(keySelector))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".IsOrderedByDescending({expression}, comparer)");
        return new CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey>(source.Context, keySelector, comparer);
    }

    /// <summary>
    /// Returns a wrapper for collection count assertions.
    /// Example: await Assert.That(list).HasCount().EqualTo(5);
    /// </summary>
    public static CountWrapper<TCollection> HasCount<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append(".HasCount()");
        return new CountWrapper<TCollection>(source.Context);
    }

    /// <summary>
    /// Asserts that the collection has the expected count.
    /// Example: await Assert.That(list).HasCount(5);
    /// </summary>
    public static CollectionCountAssertion<TCollection> HasCount<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".HasCount({expression})");
        return new CollectionCountAssertion<TCollection>(source.Context, expectedCount);
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
    public static HasSingleItemAssertion<TCollection> HasSingleItem<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append(".HasSingleItem()");
        return new HasSingleItemAssertion<TCollection>(source.Context);
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

    /// <summary>
    /// Asserts that the collection is equivalent to the expected collection.
    /// Two collections are equivalent if they contain the same elements, regardless of order.
    /// </summary>
    public static IsEquivalentToAssertion<TCollection, TItem> IsEquivalentTo<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        IEnumerable<TItem> expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".IsEquivalentTo({expression})");
        return new IsEquivalentToAssertion<TCollection, TItem>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the collection is equivalent to the expected collection using the specified comparer.
    /// </summary>
    public static IsEquivalentToAssertion<TCollection, TItem> IsEquivalentTo<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        IEnumerable<TItem> expected,
        IEqualityComparer<TItem> comparer,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".IsEquivalentTo({expression}, comparer)");
        return new IsEquivalentToAssertion<TCollection, TItem>(source.Context, expected).Using(comparer);
    }

    /// <summary>
    /// Asserts that the collection is equivalent to the expected collection with the specified ordering requirement.
    /// </summary>
    public static IsEquivalentToAssertion<TCollection, TItem> IsEquivalentTo<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        IEnumerable<TItem> expected,
        Enums.CollectionOrdering ordering,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".IsEquivalentTo({expression}, CollectionOrdering.{ordering})");
        return new IsEquivalentToAssertion<TCollection, TItem>(source.Context, expected, ordering);
    }

    /// <summary>
    /// Asserts that the collection is NOT equivalent to the expected collection.
    /// </summary>
    public static NotEquivalentToAssertion<TCollection, TItem> IsNotEquivalentTo<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        IEnumerable<TItem> expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".IsNotEquivalentTo({expression})");
        return new NotEquivalentToAssertion<TCollection, TItem>(source.Context, expected);
    }

    /// <summary>
    /// Asserts that the collection is NOT equivalent to the expected collection using the specified comparer.
    /// </summary>
    public static NotEquivalentToAssertion<TCollection, TItem> IsNotEquivalentTo<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        IEnumerable<TItem> expected,
        IEqualityComparer<TItem> comparer,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".IsNotEquivalentTo({expression}, comparer)");
        return new NotEquivalentToAssertion<TCollection, TItem>(source.Context, expected).Using(comparer);
    }

    /// <summary>
    /// Asserts that the collection is NOT equivalent to the expected collection with the specified ordering requirement.
    /// </summary>
    public static NotEquivalentToAssertion<TCollection, TItem> IsNotEquivalentTo<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source,
        IEnumerable<TItem> expected,
        Enums.CollectionOrdering ordering,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".IsNotEquivalentTo({expression}, CollectionOrdering.{ordering})");
        return new NotEquivalentToAssertion<TCollection, TItem>(source.Context, expected, ordering);
    }
}
