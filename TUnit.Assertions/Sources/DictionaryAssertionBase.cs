using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for dictionary assertions that preserves type through And/Or chains.
/// Implements ICollectionAssertionSource to enable all collection extension methods,
/// since dictionaries are collections of KeyValuePair items.
/// </summary>
/// <typeparam name="TKey">The dictionary key type</typeparam>
/// <typeparam name="TValue">The dictionary value type</typeparam>
public abstract class DictionaryAssertionBase<TKey, TValue>
    : Assertion<IReadOnlyDictionary<TKey, TValue>>,
      ICollectionAssertionSource<IReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>
{
    AssertionContext<IReadOnlyDictionary<TKey, TValue>> IAssertionSource<IReadOnlyDictionary<TKey, TValue>>.Context => Context;

    protected DictionaryAssertionBase(AssertionContext<IReadOnlyDictionary<TKey, TValue>> context)
        : base(context)
    {
    }

    protected override string GetExpectation() => "dictionary assertion";

    /// <summary>
    /// Returns an And continuation that preserves dictionary type, key type, and value type.
    /// Overrides the base Assertion.And to return a dictionary-specific continuation.
    /// </summary>
    public new DictionaryAndContinuation<TKey, TValue> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<IReadOnlyDictionary<TKey, TValue>>>();
            return new DictionaryAndContinuation<TKey, TValue>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves dictionary type, key type, and value type.
    /// Overrides the base Assertion.Or to return a dictionary-specific continuation.
    /// </summary>
    public new DictionaryOrContinuation<TKey, TValue> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<IReadOnlyDictionary<TKey, TValue>>>();
            return new DictionaryOrContinuation<TKey, TValue>(Context, InternalWrappedExecution ?? this);
        }
    }
}
