using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for all collection assertions that preserves type through And/Or chains.
/// Implements ICollectionAssertionSource to enable all collection extension methods.
/// All collection-specific operations (Contains, IsInOrder, etc.) are provided via extension methods.
/// </summary>
/// <typeparam name="TCollection">The specific collection type</typeparam>
/// <typeparam name="TItem">The type of items in the collection</typeparam>
public abstract class CollectionAssertionBase<TCollection, TItem>
    : Assertion<TCollection>,
      ICollectionAssertionSource<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
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
    /// This instance method allows single type parameter usage without needing to specify the source type.
    /// Example: await Assert.That(readOnlyList).IsTypeOf&lt;List&lt;double&gt;&gt;();
    /// </summary>
    public TypeOfAssertion<TCollection, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TCollection, TExpected>(Context);
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
    public CollectionCountAssertion<TCollection, TItem> HasCount(
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expression = null)
    {
        Context.ExpressionBuilder.Append($".HasCount({expression})");
        return new CollectionCountAssertion<TCollection, TItem>(Context, expectedCount);
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
