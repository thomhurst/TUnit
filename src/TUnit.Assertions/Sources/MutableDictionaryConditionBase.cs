using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for MutableDictionary assertions that perform a check.
/// It can be awaited and joined to the next assertion with <c>And</c> or <c>Or</c>, which keep the MutableDictionary methods.
/// It is not an assertion source, so further assertions cannot be chained without <c>And</c> or <c>Or</c>.
/// </summary>
public abstract class MutableDictionaryConditionBase<TDictionary, TKey, TValue> : CollectionConditionBase<TDictionary, KeyValuePair<TKey, TValue>>
    where TDictionary : IDictionary<TKey, TValue>
    where TKey : notnull
{
    protected MutableDictionaryConditionBase(AssertionContext<TDictionary> context)
        : base(context)
    {
    }

    /// <summary>
    /// Returns an And continuation that preserves dictionary type, key type, and value type.
    /// </summary>
    public new MutableDictionaryAndContinuation<TDictionary, TKey, TValue> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<TDictionary>>();
            return new MutableDictionaryAndContinuation<TDictionary, TKey, TValue>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves dictionary type, key type, and value type.
    /// </summary>
    public new MutableDictionaryOrContinuation<TDictionary, TKey, TValue> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<TDictionary>>();
            return new MutableDictionaryOrContinuation<TDictionary, TKey, TValue>(Context, InternalWrappedExecution ?? this);
        }
    }
}
