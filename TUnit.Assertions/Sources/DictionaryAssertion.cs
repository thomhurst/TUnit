namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for dictionary values.
/// This is the entry point for: Assert.That(dictionary)
/// Knows the TKey and TValue types, enabling better type inference for dictionary operations.
/// Inherits from ValueAssertion to get all value-based assertions like IsTypeOf without duplication.
/// </summary>
public class DictionaryAssertion<TKey, TValue> : ValueAssertion<IReadOnlyDictionary<TKey, TValue>>
{
    public DictionaryAssertion(IReadOnlyDictionary<TKey, TValue> value, string? expression)
        : base(value, expression)
    {
    }
}
