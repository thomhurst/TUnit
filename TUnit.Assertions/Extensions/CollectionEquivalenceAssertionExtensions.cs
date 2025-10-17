using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for collection equivalence assertions.
/// Includes: IsEquivalentTo, IsNotEquivalentTo
/// </summary>
public static class CollectionEquivalenceAssertionExtensions
{
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
