using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for collection ordering and sorting assertions.
/// Includes: IsOrderedBy, IsOrderedByDescending
/// </summary>
public static class CollectionOrderingAssertionExtensions
{
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
}
