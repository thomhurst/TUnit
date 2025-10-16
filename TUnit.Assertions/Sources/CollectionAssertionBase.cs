using System.Collections;
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

    protected override string GetExpectation() => "collection assertion";

    /// <summary>
    /// Returns an And continuation that preserves collection type and item type.
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
            // Check if we're chaining Or after And (mixing combiners)
            if (InternalWrappedExecution is Chaining.AndAssertion<TCollection>)
            {
                throw new Exceptions.MixedAndOrAssertionsException();
            }
            return new CollectionOrContinuation<TCollection, TItem>(Context, InternalWrappedExecution ?? this);
        }
    }
}
