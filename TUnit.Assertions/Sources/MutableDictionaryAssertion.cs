using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for IDictionary values (mutable dictionaries).
/// This is the entry point for: Assert.That(idictionary)
/// Knows the TKey and TValue types, enabling better type inference for dictionary operations.
/// Inherits from MutableDictionaryAssertionBase to get And/Or chaining with type preservation,
/// plus collection methods (Contains, IsEmpty, All, etc.) since dictionaries are collections of KeyValuePair items.
/// </summary>
public class MutableDictionaryAssertion<TKey, TValue> : MutableDictionaryAssertionBase<IDictionary<TKey, TValue>, TKey, TValue>
    where TKey : notnull
{
    public MutableDictionaryAssertion(IDictionary<TKey, TValue> value, string? expression)
        : base(CreateContext(value, expression))
    {
    }

    private static AssertionContext<IDictionary<TKey, TValue>> CreateContext(
        IDictionary<TKey, TValue> value,
        string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        return new AssertionContext<IDictionary<TKey, TValue>>(value, expressionBuilder);
    }
}
