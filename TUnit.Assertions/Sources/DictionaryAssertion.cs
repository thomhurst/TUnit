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
    public EvaluationContext<IReadOnlyDictionary<TKey, TValue>> Context { get; }
    public StringBuilder ExpressionBuilder { get; }

    public DictionaryAssertion(IReadOnlyDictionary<TKey, TValue> value, string? expression)
    {
        Context = new EvaluationContext<IReadOnlyDictionary<TKey, TValue>>(value);
        ExpressionBuilder = new StringBuilder();
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }
}
