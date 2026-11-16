using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for List assertions that perform a check.
/// It can be awaited and joined to the next assertion with <c>And</c> or <c>Or</c>, which keep the List methods.
/// It is not an assertion source, so further assertions cannot be chained without <c>And</c> or <c>Or</c>.
/// </summary>
public abstract class ListConditionBase<TList, TItem> : CollectionConditionBase<TList, TItem>
    where TList : IList<TItem>
{
    protected ListConditionBase(AssertionContext<TList> context)
        : base(context)
    {
    }

    /// <summary>
    /// Adds a custom message explaining why this assertion should pass.
    /// Keeps the typed <c>And</c>/<c>Or</c> continuations.
    /// </summary>
    public new ListConditionBase<TList, TItem> Because(string message)
    {
        base.Because(message);
        return this;
    }

    /// <summary>
    /// Returns an And continuation that preserves list type and item type.
    /// Overrides the base CollectionAssertionBase.And to return a list-specific continuation.
    /// </summary>
    public new ListAndContinuation<TList, TItem> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<TList>>();
            return new ListAndContinuation<TList, TItem>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves list type and item type.
    /// Overrides the base CollectionAssertionBase.Or to return a list-specific continuation.
    /// </summary>
    public new ListOrContinuation<TList, TItem> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<TList>>();
            return new ListOrContinuation<TList, TItem>(Context, InternalWrappedExecution ?? this);
        }
    }
}
