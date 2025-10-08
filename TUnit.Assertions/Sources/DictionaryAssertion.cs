using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for dictionary values.
/// This is the entry point for: Assert.That(dictionary)
/// Knows the TKey and TValue types, enabling better type inference for dictionary operations.
/// Does not inherit from Assertion to prevent premature awaiting.
/// </summary>
public class DictionaryAssertion<TKey, TValue> : IAssertionSource<IReadOnlyDictionary<TKey, TValue>>
{
    public AssertionContext<IReadOnlyDictionary<TKey, TValue>> Context { get; }

    public DictionaryAssertion(IReadOnlyDictionary<TKey, TValue> value, string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        Context = new AssertionContext<IReadOnlyDictionary<TKey, TValue>>(value, expressionBuilder);
    }
}
