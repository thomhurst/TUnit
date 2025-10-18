using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for dictionary values.
/// This is the entry point for: Assert.That(dictionary)
/// Knows the TKey and TValue types, enabling better type inference for dictionary operations.
/// Inherits from DictionaryAssertionBase to get And/Or chaining with type preservation,
/// plus collection methods (Contains, IsEmpty, All, etc.) since dictionaries are collections of KeyValuePair items.
/// </summary>
public class DictionaryAssertion<TKey, TValue> : DictionaryAssertionBase<IReadOnlyDictionary<TKey, TValue>, TKey, TValue>
{
    public DictionaryAssertion(IReadOnlyDictionary<TKey, TValue> value, string? expression)
        : base(CreateContext(value, expression))
    {
    }

    private static AssertionContext<IReadOnlyDictionary<TKey, TValue>> CreateContext(
        IReadOnlyDictionary<TKey, TValue> value,
        string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        return new AssertionContext<IReadOnlyDictionary<TKey, TValue>>(value, expressionBuilder);
    }
}
