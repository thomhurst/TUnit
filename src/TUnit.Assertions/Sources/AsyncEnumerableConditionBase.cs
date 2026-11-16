using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for AsyncEnumerable assertions that perform a check.
/// It can be awaited and joined to the next assertion with <c>And</c> or <c>Or</c>, which keep the AsyncEnumerable methods.
/// It is not an assertion source, so further assertions cannot be chained without <c>And</c> or <c>Or</c>.
/// </summary>
public abstract class AsyncEnumerableConditionBase<TItem> : Assertion<IAsyncEnumerable<TItem>>

{
    protected AsyncEnumerableConditionBase(AssertionContext<IAsyncEnumerable<TItem>> context)
        : base(context)
    {
    }

    /// <summary>
    /// Returns an And continuation that preserves the async enumerable type.
    /// </summary>
    public new AsyncEnumerableAndContinuation<TItem> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<IAsyncEnumerable<TItem>>>();
            return new AsyncEnumerableAndContinuation<TItem>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves the async enumerable type.
    /// </summary>
    public new AsyncEnumerableOrContinuation<TItem> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<IAsyncEnumerable<TItem>>>();
            return new AsyncEnumerableOrContinuation<TItem>(Context, InternalWrappedExecution ?? this);
        }
    }
}
