using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Base class for Dictionary assertions that perform a check.
/// It can be awaited and joined to the next assertion with <c>And</c> or <c>Or</c>, which keep the Dictionary methods.
/// It is not an assertion source, so further assertions cannot be chained without <c>And</c> or <c>Or</c>.
/// </summary>
public abstract class DictionaryConditionBase<TDictionary, TKey, TValue> : CollectionConditionBase<TDictionary, KeyValuePair<TKey, TValue>>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    protected DictionaryConditionBase(AssertionContext<TDictionary> context)
        : base(context)
    {
    }

    /// <summary>
    /// Returns an And continuation that preserves dictionary type, key type, and value type.
    /// Hides the base Assertion.And to return a dictionary-specific continuation.
    /// </summary>
    public new DictionaryAndContinuation<TDictionary, TKey, TValue> And
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.OrAssertion<TDictionary>>();
            return new DictionaryAndContinuation<TDictionary, TKey, TValue>(Context, InternalWrappedExecution ?? this);
        }
    }

    /// <summary>
    /// Returns an Or continuation that preserves dictionary type, key type, and value type.
    /// Hides the base Assertion.Or to return a dictionary-specific continuation.
    /// </summary>
    public new DictionaryOrContinuation<TDictionary, TKey, TValue> Or
    {
        get
        {
            ThrowIfMixingCombiner<Chaining.AndAssertion<TDictionary>>();
            return new DictionaryOrContinuation<TDictionary, TKey, TValue>(Context, InternalWrappedExecution ?? this);
        }
    }
}
