using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for Set assertions that perform a check.
/// It can be awaited and joined to the next assertion with <c>And</c> or <c>Or</c>, which keep the Set methods.
/// It is not an assertion source, so further assertions cannot be chained without <c>And</c> or <c>Or</c>.
/// </summary>
public abstract class SetConditionBase<TSet, TItem> : CollectionConditionBase<TSet, TItem>
    where TSet : IEnumerable<TItem>
{
    protected SetConditionBase(AssertionContext<TSet> context)
        : base(context)
    {
    }

    /// <summary>
    /// Adds a custom message explaining why this assertion should pass.
    /// Keeps the typed <c>And</c>/<c>Or</c> continuations.
    /// </summary>
    public new SetConditionBase<TSet, TItem> Because(string message)
    {
        base.Because(message);
        return this;
    }

    protected abstract ISetAdapter<TItem> CreateSetAdapter(TSet value);

    /// <summary>
    /// Returns an And continuation that preserves set type and item type.
    /// </summary>
    public new SetAndContinuation<TSet, TItem> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<TSet>>();
            return new SetAndContinuation<TSet, TItem>(Context, InternalWrappedExecution ?? this, CreateSetAdapter);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves set type and item type.
    /// </summary>
    public new SetOrContinuation<TSet, TItem> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<TSet>>();
            return new SetOrContinuation<TSet, TItem>(Context, InternalWrappedExecution ?? this, CreateSetAdapter);
        }
    }
}
