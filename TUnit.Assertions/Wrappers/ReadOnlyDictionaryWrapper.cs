using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Assertions.Wrappers;

/// <summary>
/// Wraps an IDictionary as IReadOnlyDictionary for assertion purposes.
/// Preserves the original reference for identity comparisons.
/// </summary>
internal sealed class ReadOnlyDictionaryWrapper<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    private readonly IDictionary<TKey, TValue> _dictionary;

    /// <summary>
    /// The original IDictionary reference, for IsSameReferenceAs assertions.
    /// </summary>
    public object OriginalReference => _dictionary;

    public ReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> dictionary)
        => _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

    public TValue this[TKey key] => _dictionary[key];
    public IEnumerable<TKey> Keys => _dictionary.Keys;
    public IEnumerable<TValue> Values => _dictionary.Values;
    public int Count => _dictionary.Count;
    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

#if NETSTANDARD2_0
    public bool TryGetValue(TKey key, out TValue value)
#else
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#endif
    {
        return _dictionary.TryGetValue(key, out value!);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
