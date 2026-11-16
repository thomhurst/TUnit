using TUnit.Assertions.Chaining;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for ReadOnlyList assertions that perform a check.
/// It can be awaited and joined to the next assertion with <c>And</c> or <c>Or</c>, which keep the ReadOnlyList methods.
/// It is not an assertion source, so further assertions cannot be chained without <c>And</c> or <c>Or</c>.
/// </summary>
public abstract class ReadOnlyListConditionBase<TList, TItem> : CollectionConditionBase<TList, TItem>
    where TList : IReadOnlyList<TItem>
{
    protected ReadOnlyListConditionBase(AssertionContext<TList> context)
        : base(context)
    {
    }

    /// <summary>
    /// Returns an And continuation that preserves the read-only list type.
    /// </summary>
    public new ReadOnlyListAndContinuation<TList, TItem> And
    {
        get
        {
            ThrowIfMixingCombiner<OrAssertion<TList>>();
            return new ReadOnlyListAndContinuation<TList, TItem>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves the read-only list type.
    /// </summary>
    public new ReadOnlyListOrContinuation<TList, TItem> Or
    {
        get
        {
            ThrowIfMixingCombiner<AndAssertion<TList>>();
            return new ReadOnlyListOrContinuation<TList, TItem>(Context, InternalWrappedExecution ?? this);
        }
    }
}
