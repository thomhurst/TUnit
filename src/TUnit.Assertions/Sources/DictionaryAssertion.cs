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
#if !NETSTANDARD2_0
    , IAssertionSourceFor<IReadOnlyDictionary<TKey, TValue>, DictionaryAssertion<TKey, TValue>>
#endif
    where TKey : notnull
{
    public DictionaryAssertion(IReadOnlyDictionary<TKey, TValue>? value, string? expression)
        : base(CreateContext(value, expression))
    {
    }

    /// <summary>
    /// Seeds a dictionary assertion over an existing context (used by the dictionary <c>.Value</c> drill-in
    /// when the value is itself a read-only dictionary).
    /// </summary>
    internal DictionaryAssertion(AssertionContext<IReadOnlyDictionary<TKey, TValue>> context)
        : base(context)
    {
    }

#if !NETSTANDARD2_0
    public static DictionaryAssertion<TKey, TValue> Create(IReadOnlyDictionary<TKey, TValue> item, string label) => new(item, label);
#endif

    private static AssertionContext<IReadOnlyDictionary<TKey, TValue>> CreateContext(
        IReadOnlyDictionary<TKey, TValue>? value,
        string? expression)
    {
        var expressionBuilder = AssertionExpressionBuilder.Create(expression);
        return new AssertionContext<IReadOnlyDictionary<TKey, TValue>>(value!, expressionBuilder);
    }
}
