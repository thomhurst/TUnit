using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Core;

/// <summary>
/// And continuation for dictionary assertions that preserves dictionary type, key type, and value type.
/// Inherits from DictionaryAssertionBase to automatically expose all dictionary and collection methods.
/// </summary>
public class DictionaryAndContinuation<TDictionary, TKey, TValue> : DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    internal DictionaryAndContinuation(
        AssertionContext<TDictionary> context,
        Assertion<TDictionary> previousAssertion)
        : base(context, previousAssertion, ".And", CombinerType.And)
    {
    }
}

/// <summary>
/// Or continuation for dictionary assertions that preserves dictionary type, key type, and value type.
/// Inherits from DictionaryAssertionBase to automatically expose all dictionary and collection methods.
/// </summary>
public class DictionaryOrContinuation<TDictionary, TKey, TValue> : DictionaryAssertionBase<TDictionary, TKey, TValue>
    where TDictionary : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    internal DictionaryOrContinuation(
        AssertionContext<TDictionary> context,
        Assertion<TDictionary> previousAssertion)
        : base(context, previousAssertion, ".Or", CombinerType.Or)
    {
    }
}
