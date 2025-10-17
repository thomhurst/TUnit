using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// And continuation for dictionary assertions that preserves dictionary type, key type, and value type.
/// Implements both IAssertionSource and ICollectionAssertionSource to enable dictionary-specific
/// and collection extension methods.
/// </summary>
public class DictionaryAndContinuation<TKey, TValue>
    : ContinuationBase<IReadOnlyDictionary<TKey, TValue>>,
      ICollectionAssertionSource<IReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>
{
    internal DictionaryAndContinuation(
        AssertionContext<IReadOnlyDictionary<TKey, TValue>> context,
        Assertion<IReadOnlyDictionary<TKey, TValue>> previousAssertion)
        : base(context, previousAssertion, ".And", CombinerType.And)
    {
    }
}

/// <summary>
/// Or continuation for dictionary assertions that preserves dictionary type, key type, and value type.
/// Implements both IAssertionSource and ICollectionAssertionSource to enable dictionary-specific
/// and collection extension methods.
/// </summary>
public class DictionaryOrContinuation<TKey, TValue>
    : ContinuationBase<IReadOnlyDictionary<TKey, TValue>>,
      ICollectionAssertionSource<IReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>
{
    internal DictionaryOrContinuation(
        AssertionContext<IReadOnlyDictionary<TKey, TValue>> context,
        Assertion<IReadOnlyDictionary<TKey, TValue>> previousAssertion)
        : base(context, previousAssertion, ".Or", CombinerType.Or)
    {
    }
}
