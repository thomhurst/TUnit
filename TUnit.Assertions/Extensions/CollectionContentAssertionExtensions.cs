using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for collection content and membership assertions.
/// Includes: IsEmpty, IsNotEmpty, Contains, DoesNotContain, ContainsOnly
/// </summary>
public static class CollectionContentAssertionExtensions
{
    /// <summary>
    /// Asserts that the collection is empty.
    /// </summary>
    public static CollectionIsEmptyAssertion<TCollection, TItem> IsEmpty<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append(".IsEmpty()");
        return new CollectionIsEmptyAssertion<TCollection, TItem>(source.Context);
    }

    /// <summary>
    /// Asserts that the collection is NOT empty.
    /// </summary>
    public static CollectionIsNotEmptyAssertion<TCollection, TItem> IsNotEmpty<TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return new CollectionIsNotEmptyAssertion<TCollection, TItem>(source.Context);
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
    /// Asserts that the collection is of the specified concrete type.
    /// This overload infers the collection type from the source, requiring only TExpected to be specified.
    /// Example: await Assert.That(readOnlyList).IsTypeOf&lt;List&lt;double&gt;&gt;();
    /// </summary>
    public static TypeOfAssertion<TCollection, TExpected> IsTypeOf<TExpected, TCollection, TItem>(
        this ICollectionAssertionSource<TCollection, TItem> source)
        where TCollection : IEnumerable<TItem>
    {
        source.Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TCollection, TExpected>(source.Context);
    }
}
