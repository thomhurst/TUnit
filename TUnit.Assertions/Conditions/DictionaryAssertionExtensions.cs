using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for dictionary types using [GenerateAssertion] attributes.
/// These wrap dictionary checks as extension methods.
/// </summary>
file static partial class DictionaryAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to contain key {expectedKey}", InlineMethodBody = true)]
    public static bool ContainsKey<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary,
        TKey expectedKey) => dictionary.ContainsKey(expectedKey);

    [GenerateAssertion(ExpectationMessage = "to not contain key {expectedKey}", InlineMethodBody = true)]
    public static bool DoesNotContainKey<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary,
        TKey expectedKey) => !dictionary.ContainsKey(expectedKey);

    [GenerateAssertion(ExpectationMessage = "to contain value {expectedValue}", InlineMethodBody = true)]
    public static bool ContainsValue<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary,
        TValue expectedValue) => dictionary.Values.Contains(expectedValue);

    [GenerateAssertion(ExpectationMessage = "to not contain value {expectedValue}", InlineMethodBody = true)]
    public static bool DoesNotContainValue<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary,
        TValue expectedValue) => !dictionary.Values.Contains(expectedValue);
}
