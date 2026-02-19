using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Conditions.Wrappers;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for all collection assertions that preserves type through And/Or chains.
/// Implements IAssertionSource&lt;TCollection&gt; to enable all collection and value extension methods.
/// All collection-specific operations (Contains, IsInOrder, etc.) are provided as instance methods.
/// </summary>
/// <typeparam name="TCollection">The concrete collection type (e.g., IEnumerable&lt;T&gt;, IReadOnlyDictionary&lt;K,V&gt;)</typeparam>
/// <typeparam name="TItem">The type of items in the collection</typeparam>
public abstract class CollectionAssertionBase<TCollection, TItem> : Assertion<TCollection>, IAssertionSource<TCollection>
    where TCollection : IEnumerable<TItem>
{
    /// <summary>
    /// Explicit implementation of IAssertionSource.Context to expose the context publicly.
    /// </summary>
    AssertionContext<TCollection> IAssertionSource<TCollection>.Context => Context;

    protected CollectionAssertionBase(AssertionContext<TCollection> context)
        : base(context)
    {
    }

    /// <summary>
    /// Constructor for continuation classes (CollectionAndContinuation, CollectionOrContinuation).
    /// Handles linking to previous assertion and appending combiner expression.
    /// Private protected means accessible only to derived classes within the same assembly.
    /// </summary>
    private protected CollectionAssertionBase(
        AssertionContext<TCollection> context,
        Assertion<TCollection> previousAssertion,
        string combinerExpression,
        CombinerType combinerType)
        : base(context)
    {
        context.ExpressionBuilder.Append(combinerExpression);
        context.SetPendingLink(previousAssertion, combinerType);
    }

    protected override string GetExpectation() => "collection assertion";

    /// <summary>
    /// Asserts that the collection is of the specified type and returns an assertion on the casted value.
    /// This allows chaining additional assertions on the typed value.
    /// Example: await Assert.That((IEnumerable<int>)list).IsTypeOf<List<int>>().And.HasCount(5);
    /// </summary>
    public TypeOfAssertion<TCollection, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TCollection, TExpected>(Context);
    }

    public IsAssignableToAssertion<TTarget, TCollection> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, TCollection>(Context);
    }

    public IsNotAssignableToAssertion<TTarget, TCollection> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, TCollection>(Context);
    }

    /// <summary>
    /// Asserts that the collection is NOT of the specified type.
    /// This allows chaining additional assertions on the value.
    /// Example: await Assert.That((IEnumerable<int>)list).IsNotTypeOf<HashSet<int>>().And.HasCount(5);
    /// </summary>
    public IsNotTypeOfAssertion<TCollection, TExpected> IsNotTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<TCollection, TExpected>(Context);
    }

    /// <summary>
    /// Asserts that the collection contains the expected item.
    /// This instance method enables calling Contains with proper type inference.
    /// Example: await Assert.That(list).Contains("value");
    /// </summary>
    public CollectionContainsAssertion<TCollection, TItem> Contains(
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Contains({expression})");
        return new CollectionContainsAssertion<TCollection, TItem>(Context, expected);
    }

    /// <summary>
    /// Asserts that the collection contains the expected item using a custom equality comparer.
    /// This instance method enables calling Contains with proper type inference and a custom comparer.
    /// Example: await Assert.That(list).Contains("value", StringComparer.OrdinalIgnoreCase);
    /// </summary>
    public CollectionContainsAssertion<TCollection, TItem> Contains(
        TItem expected,
        IEqualityComparer<TItem> comparer,
        [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null,
        [CallerArgumentExpression(nameof(comparer))] string? comparerExpression = null)
    {
        Context.ExpressionBuilder.Append($".Contains({expectedExpression}, {comparerExpression})");
        return new CollectionContainsAssertion<TCollection, TItem>(Context, expected, comparer);
    }

    /// <summary>
    /// Asserts that the collection contains an item matching the predicate.
    /// This instance method enables calling Contains with proper type inference.
    /// Example: await Assert.That(list).Contains(x => x > 5);
    /// </summary>
    public CollectionContainsPredicateAssertion<TCollection, TItem> Contains(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Contains({expression})");
        return new CollectionContainsPredicateAssertion<TCollection, TItem>(Context, predicate);
    }

    /// <summary>
    /// Asserts that the collection has the expected count.
    /// This instance method enables calling HasCount with proper type inference.
    /// Example: await Assert.That(list).HasCount(5);
    /// </summary>
    [Obsolete("Use Count().IsEqualTo(expectedCount) instead.")]
    public CollectionCountAssertion<TCollection, TItem> HasCount(
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HasCount({expression})");
        return new CollectionCountAssertion<TCollection, TItem>(Context, expectedCount);
    }

    /// <summary>
    /// Returns a wrapper for fluent count assertions.
    /// This enables the pattern: .HasCount().GreaterThan(5)
    /// Example: await Assert.That(list).HasCount().EqualTo(5);
    /// </summary>
    [Obsolete("Use Count() instead, which provides all numeric assertion methods. Example: Assert.That(list).Count().IsGreaterThan(5)")]
    public CountWrapper<TCollection, TItem> HasCount()
    {
        Context.ExpressionBuilder.Append(".HasCount()");
        return new CountWrapper<TCollection, TItem>(Context);
    }

    /// <summary>
    /// Gets the count of items in the collection for further numeric assertions.
    /// This enables fluent assertions on the count itself while preserving collection type for chaining.
    /// Example: await Assert.That(list).Count().IsGreaterThan(5).And.Contains(1);
    /// </summary>
    public CollectionCountSource<TCollection, TItem> Count()
    {
        Context.ExpressionBuilder.Append(".Count()");
        return new CollectionCountSource<TCollection, TItem>(Context, null);
    }

    /// <summary>
    /// Asserts on the count of items using an inline assertion lambda.
    /// This enables compact count assertions while preserving collection type for chaining.
    /// Note: For collections of int, use Count().IsEqualTo(5) instead of Count(c => c.IsEqualTo(5))
    /// to avoid ambiguity with item-filtering.
    /// Example: await Assert.That(list).Count(c => c.IsEqualTo(5)).And.Contains(1);
    /// </summary>
    [OverloadResolutionPriority(-1)]
    public CollectionCountWithInlineAssertionAssertion<TCollection, TItem> Count(
        Func<IAssertionSource<int>, Assertion<int>?> countAssertion,
        [CallerArgumentExpression(nameof(countAssertion))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Count({expression})");
        return new CollectionCountWithInlineAssertionAssertion<TCollection, TItem>(Context, countAssertion);
    }

    /// <summary>
    /// Gets the count of items satisfying the given assertion for further numeric assertions.
    /// This enables fluent assertions on filtered counts using the full assertion builder.
    /// The result preserves collection type for further chaining.
    /// Example: await Assert.That(list).Count(item => item.IsGreaterThan(10)).IsEqualTo(3).And.Contains(1);
    /// </summary>
    public CollectionCountSource<TCollection, TItem> Count(
        Func<IAssertionSource<TItem>, Assertion<TItem>?> itemAssertion,
        [CallerArgumentExpression(nameof(itemAssertion))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Count({expression})");
        return new CollectionCountSource<TCollection, TItem>(Context, itemAssertion);
    }

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in ascending order.
    /// This instance method enables calling IsOrderedBy with proper type inference.
    /// Example: await Assert.That(list).IsOrderedBy(x => x.Name);
    /// </summary>
    public CollectionIsOrderedByAssertion<TCollection, TItem, TKey> IsOrderedBy<TKey>(
        Func<TItem, TKey> keySelector,
        [CallerArgumentExpression(nameof(keySelector))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsOrderedBy({expression})");
        return new CollectionIsOrderedByAssertion<TCollection, TItem, TKey>(Context, keySelector);
    }

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in ascending order using a custom comparer.
    /// This instance method enables calling IsOrderedBy with proper type inference.
    /// Example: await Assert.That(list).IsOrderedBy(x => x.Name, StringComparer.OrdinalIgnoreCase);
    /// </summary>
    public CollectionIsOrderedByAssertion<TCollection, TItem, TKey> IsOrderedBy<TKey>(
        Func<TItem, TKey> keySelector,
        IComparer<TKey>? comparer,
        [CallerArgumentExpression(nameof(keySelector))] string? selectorExpression = null,
        [CallerArgumentExpression(nameof(comparer))] string? comparerExpression = null)
    {
        Context.ExpressionBuilder.Append($".IsOrderedBy({selectorExpression}, {comparerExpression})");
        return new CollectionIsOrderedByAssertion<TCollection, TItem, TKey>(Context, keySelector, comparer);
    }

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in descending order.
    /// This instance method enables calling IsOrderedByDescending with proper type inference.
    /// Example: await Assert.That(list).IsOrderedByDescending(x => x.Age);
    /// </summary>
    public CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey> IsOrderedByDescending<TKey>(
        Func<TItem, TKey> keySelector,
        [CallerArgumentExpression(nameof(keySelector))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsOrderedByDescending({expression})");
        return new CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey>(Context, keySelector);
    }

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in descending order using a custom comparer.
    /// This instance method enables calling IsOrderedByDescending with proper type inference.
    /// Example: await Assert.That(list).IsOrderedByDescending(x => x.Name, StringComparer.OrdinalIgnoreCase);
    /// </summary>
    public CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey> IsOrderedByDescending<TKey>(
        Func<TItem, TKey> keySelector,
        IComparer<TKey>? comparer,
        [CallerArgumentExpression(nameof(keySelector))] string? selectorExpression = null,
        [CallerArgumentExpression(nameof(comparer))] string? comparerExpression = null)
    {
        Context.ExpressionBuilder.Append($".IsOrderedByDescending({selectorExpression}, {comparerExpression})");
        return new CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey>(Context, keySelector, comparer);
    }

    /// <summary>
    /// Asserts that the collection is empty.
    /// This instance method enables calling IsEmpty with proper type inference.
    /// Example: await Assert.That(list).IsEmpty();
    /// </summary>
    public CollectionIsEmptyAssertion<TCollection, TItem> IsEmpty()
    {
        Context.ExpressionBuilder.Append(".IsEmpty()");
        return new CollectionIsEmptyAssertion<TCollection, TItem>(Context);
    }

    /// <summary>
    /// Asserts that the collection is not empty.
    /// This instance method enables calling IsNotEmpty with proper type inference.
    /// Example: await Assert.That(list).IsNotEmpty();
    /// </summary>
    public CollectionIsNotEmptyAssertion<TCollection, TItem> IsNotEmpty()
    {
        Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return new CollectionIsNotEmptyAssertion<TCollection, TItem>(Context);
    }

    /// <summary>
    /// Asserts that the collection contains exactly one item.
    /// This instance method enables calling HasSingleItem with proper type inference.
    /// Example: await Assert.That(list).HasSingleItem();
    /// </summary>
    public HasSingleItemAssertion<TCollection, TItem> HasSingleItem()
    {
        Context.ExpressionBuilder.Append(".HasSingleItem()");
        return new HasSingleItemAssertion<TCollection, TItem>(Context);
    }

    /// <summary>
    /// Asserts that the collection contains exactly one item matching the predicate.
    /// This instance method enables calling HasSingleItem with proper type inference.
    /// Example: await Assert.That(list).HasSingleItem(x => x > 5);
    /// </summary>
    public HasSingleItemPredicateAssertion<TCollection, TItem> HasSingleItem(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HasSingleItem({expression})");
        return new HasSingleItemPredicateAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that the collection contains only distinct (unique) items.
    /// This instance method enables calling HasDistinctItems with proper type inference.
    /// Example: await Assert.That(list).HasDistinctItems();
    /// </summary>
    public HasDistinctItemsAssertion<TCollection, TItem> HasDistinctItems()
    {
        Context.ExpressionBuilder.Append(".HasDistinctItems()");
        return new HasDistinctItemsAssertion<TCollection, TItem>(Context);
    }

    /// <summary>
    /// Asserts that the collection contains only distinct (unique) items using a custom equality comparer.
    /// This instance method enables calling HasDistinctItems with proper type inference and a custom comparer.
    /// Example: await Assert.That(list).HasDistinctItems(StringComparer.OrdinalIgnoreCase);
    /// </summary>
    public HasDistinctItemsAssertion<TCollection, TItem> HasDistinctItems(
        IEqualityComparer<TItem> comparer,
        [CallerArgumentExpression(nameof(comparer))] string? comparerExpression = null)
    {
        Context.ExpressionBuilder.Append($".HasDistinctItems({comparerExpression})");
        return new HasDistinctItemsAssertion<TCollection, TItem>(Context, comparer);
    }

    /// <summary>
    /// Asserts that the collection does not contain the specified item.
    /// This instance method enables calling DoesNotContain with proper type inference.
    /// Example: await Assert.That(list).DoesNotContain("value");
    /// </summary>
    public CollectionDoesNotContainAssertion<TCollection, TItem> DoesNotContain(
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotContain({expression})");
        return new CollectionDoesNotContainAssertion<TCollection, TItem>(Context, expected);
    }

    /// <summary>
    /// Asserts that the collection does not contain the specified item using a custom equality comparer.
    /// This instance method enables calling DoesNotContain with proper type inference and a custom comparer.
    /// Example: await Assert.That(list).DoesNotContain("value", StringComparer.OrdinalIgnoreCase);
    /// </summary>
    public CollectionDoesNotContainAssertion<TCollection, TItem> DoesNotContain(
        TItem expected,
        IEqualityComparer<TItem> comparer,
        [CallerArgumentExpression(nameof(expected))] string? expectedExpression = null,
        [CallerArgumentExpression(nameof(comparer))] string? comparerExpression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotContain({expectedExpression}, {comparerExpression})");
        return new CollectionDoesNotContainAssertion<TCollection, TItem>(Context, expected, comparer);
    }

    /// <summary>
    /// Asserts that the collection does not contain any item matching the predicate.
    /// This instance method enables calling DoesNotContain with proper type inference.
    /// Example: await Assert.That(list).DoesNotContain(x => x > 10);
    /// </summary>
    public CollectionDoesNotContainPredicateAssertion<TCollection, TItem> DoesNotContain(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotContain({expression})");
        return new CollectionDoesNotContainPredicateAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that all items in the collection satisfy the predicate.
    /// This instance method enables calling All with proper type inference.
    /// Example: await Assert.That(list).All(x => x > 0);
    /// </summary>
    public CollectionAllAssertion<TCollection, TItem> All(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".All({expression})");
        return new CollectionAllAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Returns a helper for the .All().Satisfy() pattern.
    /// This instance method enables calling All().Satisfy() with proper type inference.
    /// Example: await Assert.That(list).All().Satisfy(item => item.IsNotNull());
    /// </summary>
    public CollectionAllSatisfyHelper<TCollection, TItem> All()
    {
        Context.ExpressionBuilder.Append(".All()");
        return new CollectionAllSatisfyHelper<TCollection, TItem>(Context);
    }

    /// <summary>
    /// Asserts that at least one item in the collection satisfies the predicate.
    /// This instance method enables calling Any with proper type inference.
    /// Example: await Assert.That(list).Any(x => x > 10);
    /// </summary>
    public CollectionAnyAssertion<TCollection, TItem> Any(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Any({expression})");
        return new CollectionAnyAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that the collection contains only items matching the predicate (all items must match).
    /// This instance method enables calling ContainsOnly with proper type inference.
    /// Example: await Assert.That(list).ContainsOnly(x => x > 0);
    /// </summary>
    public CollectionAllAssertion<TCollection, TItem> ContainsOnly(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".ContainsOnly({expression})");
        return new CollectionAllAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that the collection is in ascending order.
    /// This instance method enables calling IsInOrder with pure type inference.
    /// Uses runtime comparison via Comparer&lt;TItem&gt;.Default.
    /// Example: await Assert.That(list).IsInOrder();
    /// </summary>
    public CollectionIsInOrderAssertion<TCollection, TItem> IsInOrder()
    {
        Context.ExpressionBuilder.Append(".IsInOrder()");
        return new CollectionIsInOrderAssertion<TCollection, TItem>(Context);
    }

    /// <summary>
    /// Asserts that the collection is in descending order.
    /// This instance method enables calling IsInDescendingOrder with pure type inference.
    /// Uses runtime comparison via Comparer&lt;TItem&gt;.Default.
    /// Example: await Assert.That(list).IsInDescendingOrder();
    /// </summary>
    public CollectionIsInDescendingOrderAssertion<TCollection, TItem> IsInDescendingOrder()
    {
        Context.ExpressionBuilder.Append(".IsInDescendingOrder()");
        return new CollectionIsInDescendingOrderAssertion<TCollection, TItem>(Context);
    }

    /// <summary>
    /// Returns an And continuation that preserves collection type and item type.
    /// Overrides the base Assertion.And to return a collection-specific continuation.
    /// </summary>
    public new CollectionAndContinuation<TCollection, TItem> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<TCollection>>();
            return new CollectionAndContinuation<TCollection, TItem>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves collection type and item type.
    /// Overrides the base Assertion.Or to return a collection-specific continuation.
    /// </summary>
    public new CollectionOrContinuation<TCollection, TItem> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<TCollection>>();
            return new CollectionOrContinuation<TCollection, TItem>(Context, InternalWrappedExecution ?? this);
        }
    }
}
