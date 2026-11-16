#if NET5_0_OR_GREATER
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for Memory assertions that perform a check.
/// It can be awaited and joined to the next assertion with <c>And</c> or <c>Or</c>, which keep the Memory methods.
/// It is not an assertion source, so further assertions cannot be chained without <c>And</c> or <c>Or</c>.
/// </summary>
public abstract class MemoryConditionBase<TMemory, TItem> : Assertion<TMemory>

{
    protected MemoryConditionBase(AssertionContext<TMemory> context)
        : base(context)
    {
    }

    /// <summary>
    /// Adds a custom message explaining why this assertion should pass.
    /// Keeps the typed <c>And</c>/<c>Or</c> continuations.
    /// </summary>
    public new MemoryConditionBase<TMemory, TItem> Because(string message)
    {
        base.Because(message);
        return this;
    }

    protected abstract ICollectionAdapter<TItem> CreateAdapter(TMemory value);

    public new MemoryAndContinuation<TMemory, TItem> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<TMemory>>();
            return new MemoryAndContinuation<TMemory, TItem>(Context, InternalWrappedExecution ?? this, CreateAdapter);
        }
    }

    public new MemoryOrContinuation<TMemory, TItem> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<TMemory>>();
            return new MemoryOrContinuation<TMemory, TItem>(Context, InternalWrappedExecution ?? this, CreateAdapter);
        }
    }
}
#endif
