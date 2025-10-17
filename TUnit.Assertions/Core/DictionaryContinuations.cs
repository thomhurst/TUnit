using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// And continuation for dictionary assertions that preserves dictionary type, key type, and value type.
/// Inherits from DictionaryAssertionBase to automatically expose all dictionary and collection methods.
/// </summary>
public class DictionaryAndContinuation<TKey, TValue> : DictionaryAssertionBase<TKey, TValue>
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
/// Inherits from DictionaryAssertionBase to automatically expose all dictionary and collection methods.
/// </summary>
public class DictionaryOrContinuation<TKey, TValue> : DictionaryAssertionBase<TKey, TValue>
{
    internal DictionaryOrContinuation(
        AssertionContext<IReadOnlyDictionary<TKey, TValue>> context,
        Assertion<IReadOnlyDictionary<TKey, TValue>> previousAssertion)
        : base(context, previousAssertion, ".Or", CombinerType.Or)
    {
    }
}
