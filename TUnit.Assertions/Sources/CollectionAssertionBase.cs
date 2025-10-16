using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Conditions.Wrappers;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for all collection assertions that preserves type through And/Or chains.
/// Provides instance methods for collection-specific operations like Contains, IsInOrder, etc.
/// All derived collection assertions inherit these methods and can chain them fluently.
/// Also provides IsTypeOf, IsAssignableTo, and other general assertion methods.
/// </summary>
/// <typeparam name="TCollection">The specific collection type</typeparam>
/// <typeparam name="TItem">The type of items in the collection</typeparam>
public abstract class CollectionAssertionBase<TCollection, TItem>
    : Assertion<TCollection>,
      IAssertionSource<TCollection>
    where TCollection : IEnumerable<TItem>
{
    AssertionContext<TCollection> IAssertionSource<TCollection>.Context => Context;

    protected CollectionAssertionBase(AssertionContext<TCollection> context)
        : base(context)
    {
    }

    protected override string GetExpectation() => "collection assertion";

    // ============ VALUE ASSERTION METHODS (from ValueAssertion) ============

    /// <summary>
    /// Asserts that the value is of the specified type and returns an assertion on the casted value.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(myList).IsTypeOf<List<string>>();
    /// </summary>
    public TypeOfAssertion<TCollection, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TCollection, TExpected>(Context);
    }

    /// <summary>
    /// Asserts that the value's type is assignable to the specified type.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(myObject).IsAssignableTo<IDisposable>();
    /// </summary>
    public IsAssignableToAssertion<TTarget, TCollection> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, TCollection>(Context);
    }

    /// <summary>
    /// Asserts that the value's type is NOT assignable to the specified type.
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(myObject).IsNotAssignableTo<IDisposable>();
    /// </summary>
    public IsNotAssignableToAssertion<TTarget, TCollection> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, TCollection>(Context);
    }

    // ============ COLLECTION INSTANCE METHODS ============

    /// <summary>
    /// Asserts that the collection is empty.
    /// </summary>
    public virtual CollectionIsEmptyAssertion<TCollection> IsEmpty()
    {
        Context.ExpressionBuilder.Append(".IsEmpty()");
        return new CollectionIsEmptyAssertion<TCollection>(Context);
    }

    /// <summary>
    /// Asserts that the collection is NOT empty.
    /// </summary>
    public virtual CollectionIsNotEmptyAssertion<TCollection> IsNotEmpty()
    {
        Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return new CollectionIsNotEmptyAssertion<TCollection>(Context);
    }

    /// <summary>
    /// Asserts that the collection contains the expected item.
    /// </summary>
    public virtual CollectionContainsAssertion<TCollection, TItem> Contains(
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Contains({expression})");
        return new CollectionContainsAssertion<TCollection, TItem>(Context, expected);
    }

    /// <summary>
    /// Asserts that the collection contains an item matching the predicate.
    /// </summary>
    public virtual CollectionContainsPredicateAssertion<TCollection, TItem> Contains(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Contains({expression})");
        return new CollectionContainsPredicateAssertion<TCollection, TItem>(Context, predicate);
    }

    /// <summary>
    /// Asserts that the collection does NOT contain the expected item.
    /// </summary>
    public virtual CollectionDoesNotContainAssertion<TCollection, TItem> DoesNotContain(
        TItem expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotContain({expression})");
        return new CollectionDoesNotContainAssertion<TCollection, TItem>(Context, expected);
    }

    /// <summary>
    /// Asserts that the collection does NOT contain any item matching the predicate.
    /// </summary>
    public virtual CollectionDoesNotContainPredicateAssertion<TCollection, TItem> DoesNotContain(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".DoesNotContain({expression})");
        return new CollectionDoesNotContainPredicateAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that the collection contains ONLY items matching the predicate (all items satisfy the predicate).
    /// </summary>
    public virtual CollectionAllAssertion<TCollection, TItem> ContainsOnly(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".ContainsOnly({expression})");
        return new CollectionAllAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate");
    }

    // Note: IsInOrder() and IsInDescendingOrder() must remain as extension methods
    // due to their IComparable<TItem> constraint. Instance methods cannot have
    // where clauses, and we can't add the constraint to the class level because
    // not all collections have comparable items.

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in ascending order.
    /// </summary>
    public virtual CollectionIsOrderedByAssertion<TCollection, TItem, TKey> IsOrderedBy<TKey>(
        Func<TItem, TKey> keySelector,
        [CallerArgumentExpression(nameof(keySelector))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsOrderedBy({expression})");
        return new CollectionIsOrderedByAssertion<TCollection, TItem, TKey>(Context, keySelector);
    }

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in ascending order using the specified comparer.
    /// </summary>
    public virtual CollectionIsOrderedByAssertion<TCollection, TItem, TKey> IsOrderedBy<TKey>(
        Func<TItem, TKey> keySelector,
        IComparer<TKey> comparer,
        [CallerArgumentExpression(nameof(keySelector))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsOrderedBy({expression}, comparer)");
        return new CollectionIsOrderedByAssertion<TCollection, TItem, TKey>(Context, keySelector, comparer);
    }

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in descending order.
    /// </summary>
    public virtual CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey> IsOrderedByDescending<TKey>(
        Func<TItem, TKey> keySelector,
        [CallerArgumentExpression(nameof(keySelector))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsOrderedByDescending({expression})");
        return new CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey>(Context, keySelector);
    }

    /// <summary>
    /// Asserts that the collection is ordered by the specified key selector in descending order using the specified comparer.
    /// </summary>
    public virtual CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey> IsOrderedByDescending<TKey>(
        Func<TItem, TKey> keySelector,
        IComparer<TKey> comparer,
        [CallerArgumentExpression(nameof(keySelector))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsOrderedByDescending({expression}, comparer)");
        return new CollectionIsOrderedByDescendingAssertion<TCollection, TItem, TKey>(Context, keySelector, comparer);
    }

    /// <summary>
    /// Returns a wrapper for collection count assertions.
    /// Example: await Assert.That(list).HasCount().EqualTo(5);
    /// </summary>
    public virtual CountWrapper<TCollection> HasCount()
    {
        Context.ExpressionBuilder.Append(".HasCount()");
        return new CountWrapper<TCollection>(Context);
    }

    /// <summary>
    /// Asserts that the collection has the expected count.
    /// Example: await Assert.That(list).HasCount(5);
    /// </summary>
    public virtual CollectionCountAssertion<TCollection> HasCount(
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HasCount({expression})");
        return new CollectionCountAssertion<TCollection>(Context, expectedCount);
    }

    /// <summary>
    /// Creates a helper for asserting that all items in the collection satisfy custom assertions.
    /// Example: await Assert.That(list).All().Satisfy(item => item.IsNotNull());
    /// </summary>
    public virtual CollectionAllSatisfyHelper<TCollection, TItem> All()
    {
        Context.ExpressionBuilder.Append(".All()");
        return new CollectionAllSatisfyHelper<TCollection, TItem>(Context);
    }

    /// <summary>
    /// Asserts that all items in the collection satisfy the predicate.
    /// </summary>
    public virtual CollectionAllAssertion<TCollection, TItem> All(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".All({expression})");
        return new CollectionAllAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that at least one item in the collection satisfies the predicate.
    /// </summary>
    public virtual CollectionAnyAssertion<TCollection, TItem> Any(
        Func<TItem, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".Any({expression})");
        return new CollectionAnyAssertion<TCollection, TItem>(Context, predicate, expression ?? "predicate");
    }

    /// <summary>
    /// Asserts that the collection contains exactly one item.
    /// </summary>
    public virtual HasSingleItemAssertion<TCollection> HasSingleItem()
    {
        Context.ExpressionBuilder.Append(".HasSingleItem()");
        return new HasSingleItemAssertion<TCollection>(Context);
    }

    /// <summary>
    /// Asserts that the collection contains only distinct (unique) items.
    /// </summary>
    public virtual HasDistinctItemsAssertion<TCollection> HasDistinctItems()
    {
        Context.ExpressionBuilder.Append(".HasDistinctItems()");
        return new HasDistinctItemsAssertion<TCollection>(Context);
    }

    /// <summary>
    /// Asserts that the collection is equivalent to the expected collection.
    /// Two collections are equivalent if they contain the same elements, regardless of order.
    /// </summary>
    public virtual IsEquivalentToAssertion<TCollection, TItem> IsEquivalentTo(
        IEnumerable<TItem> expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsEquivalentTo({expression})");
        return new IsEquivalentToAssertion<TCollection, TItem>(Context, expected);
    }

    /// <summary>
    /// Asserts that the collection is equivalent to the expected collection using the specified comparer.
    /// </summary>
    public virtual IsEquivalentToAssertion<TCollection, TItem> IsEquivalentTo(
        IEnumerable<TItem> expected,
        IEqualityComparer<TItem> comparer,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsEquivalentTo({expression}, comparer)");
        return new IsEquivalentToAssertion<TCollection, TItem>(Context, expected).Using(comparer);
    }

    /// <summary>
    /// Asserts that the collection is equivalent to the expected collection with the specified ordering requirement.
    /// </summary>
    public virtual IsEquivalentToAssertion<TCollection, TItem> IsEquivalentTo(
        IEnumerable<TItem> expected,
        Enums.CollectionOrdering ordering,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsEquivalentTo({expression}, CollectionOrdering.{ordering})");
        return new IsEquivalentToAssertion<TCollection, TItem>(Context, expected, ordering);
    }

    /// <summary>
    /// Asserts that the collection is NOT equivalent to the expected collection.
    /// </summary>
    public virtual NotEquivalentToAssertion<TCollection, TItem> IsNotEquivalentTo(
        IEnumerable<TItem> expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsNotEquivalentTo({expression})");
        return new NotEquivalentToAssertion<TCollection, TItem>(Context, expected);
    }

    /// <summary>
    /// Asserts that the collection is NOT equivalent to the expected collection using the specified comparer.
    /// </summary>
    public virtual NotEquivalentToAssertion<TCollection, TItem> IsNotEquivalentTo(
        IEnumerable<TItem> expected,
        IEqualityComparer<TItem> comparer,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsNotEquivalentTo({expression}, comparer)");
        return new NotEquivalentToAssertion<TCollection, TItem>(Context, expected).Using(comparer);
    }

    /// <summary>
    /// Asserts that the collection is NOT equivalent to the expected collection with the specified ordering requirement.
    /// </summary>
    public virtual NotEquivalentToAssertion<TCollection, TItem> IsNotEquivalentTo(
        IEnumerable<TItem> expected,
        Enums.CollectionOrdering ordering,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".IsNotEquivalentTo({expression}, CollectionOrdering.{ordering})");
        return new NotEquivalentToAssertion<TCollection, TItem>(Context, expected, ordering);
    }

    // ============ AND/OR CONTINUATIONS THAT PRESERVE COLLECTION METHODS ============

    /// <summary>
    /// Returns an And continuation that preserves collection instance methods.
    /// Overrides the base Assertion.And to return a collection-specific continuation.
    /// </summary>
    public new CollectionAndContinuation<TCollection, TItem> And
    {
        get
        {
            // Check if we're chaining And after Or (mixing combiners)
            if (InternalWrappedExecution is Chaining.OrAssertion<TCollection>)
            {
                throw new Exceptions.MixedAndOrAssertionsException();
            }
            return new CollectionAndContinuation<TCollection, TItem>(Context, this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves collection instance methods.
    /// Overrides the base Assertion.Or to return a collection-specific continuation.
    /// </summary>
    public new CollectionOrContinuation<TCollection, TItem> Or
    {
        get
        {
            // Check if we're chaining Or after And (mixing combiners)
            if (InternalWrappedExecution is Chaining.AndAssertion<TCollection>)
            {
                throw new Exceptions.MixedAndOrAssertionsException();
            }
            return new CollectionOrContinuation<TCollection, TItem>(Context, this);
        }
    }
}
