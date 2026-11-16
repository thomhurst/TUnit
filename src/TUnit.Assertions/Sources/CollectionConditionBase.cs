using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for Collection assertions that perform a check.
/// It can be awaited and joined to the next assertion with <c>And</c> or <c>Or</c>, which keep the Collection methods.
/// It is not an assertion source, so further assertions cannot be chained without <c>And</c> or <c>Or</c>.
/// </summary>
public abstract class CollectionConditionBase<TCollection, TItem> : Assertion<TCollection>
    where TCollection : IEnumerable<TItem>
{
    protected CollectionConditionBase(AssertionContext<TCollection> context)
        : base(context)
    {
    }

    /// <summary>
    /// Adds a custom message explaining why this assertion should pass.
    /// Keeps the typed <c>And</c>/<c>Or</c> continuations.
    /// </summary>
    public new CollectionConditionBase<TCollection, TItem> Because(string message)
    {
        base.Because(message);
        return this;
    }

    /// <summary>
    /// Returns an And continuation that preserves collection type and item type.
    /// Hides the base Assertion.And to return a collection-specific continuation.
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
    /// Hides the base Assertion.Or to return a collection-specific continuation.
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
