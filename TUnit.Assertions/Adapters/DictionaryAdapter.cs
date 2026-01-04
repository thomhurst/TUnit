using TUnit.Assertions.Abstractions;

namespace TUnit.Assertions.Adapters;

/// <summary>
/// Zero-allocation struct adapter for IDictionary&lt;TKey, TValue&gt;.
/// Provides unified access to dictionary operations for assertion logic.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public readonly struct DictionaryAdapter<TKey, TValue> : IDictionaryAdapter<TKey, TValue>
    where TKey : notnull
{
    private readonly IDictionary<TKey, TValue>? _source;

    /// <summary>
    /// Creates a new adapter for the specified dictionary.
    /// </summary>
    public DictionaryAdapter(IDictionary<TKey, TValue>? source)
    {
        _source = source;
    }

    /// <inheritdoc />
    public int Count => _source?.Count ?? 0;

    /// <inheritdoc />
    public bool IsEmpty => _source == null || _source.Count == 0;

    /// <inheritdoc />
    public string? Description => null;

    /// <inheritdoc />
    public IEnumerable<TKey> Keys => _source?.Keys ?? Enumerable.Empty<TKey>();

    /// <inheritdoc />
    public IEnumerable<TValue> Values => _source?.Values ?? Enumerable.Empty<TValue>();

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable()
        => _source ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>();

    /// <inheritdoc />
    public bool ContainsKey(TKey key)
        => _source?.ContainsKey(key) ?? false;

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue? value)
    {
        if (_source != null && _source.TryGetValue(key, out var val))
        {
            value = val;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public bool ContainsValue(TValue value, IEqualityComparer<TValue>? comparer = null)
    {
        if (_source == null)
        {
            return false;
        }

        comparer ??= EqualityComparer<TValue>.Default;
        foreach (var v in _source.Values)
        {
            if (comparer.Equals(v, value))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Zero-allocation struct adapter for IReadOnlyDictionary&lt;TKey, TValue&gt;.
/// Provides unified access to dictionary operations for assertion logic.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public readonly struct ReadOnlyDictionaryAdapter<TKey, TValue> : IDictionaryAdapter<TKey, TValue>
    where TKey : notnull
{
    private readonly IReadOnlyDictionary<TKey, TValue>? _source;

    /// <summary>
    /// Creates a new adapter for the specified read-only dictionary.
    /// </summary>
    public ReadOnlyDictionaryAdapter(IReadOnlyDictionary<TKey, TValue>? source)
    {
        _source = source;
    }

    /// <inheritdoc />
    public int Count => _source?.Count ?? 0;

    /// <inheritdoc />
    public bool IsEmpty => _source == null || _source.Count == 0;

    /// <inheritdoc />
    public string? Description => null;

    /// <inheritdoc />
    public IEnumerable<TKey> Keys => _source?.Keys ?? Enumerable.Empty<TKey>();

    /// <inheritdoc />
    public IEnumerable<TValue> Values => _source?.Values ?? Enumerable.Empty<TValue>();

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable()
        => _source ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>();

    /// <inheritdoc />
    public bool ContainsKey(TKey key)
        => _source?.ContainsKey(key) ?? false;

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue? value)
    {
        if (_source != null && _source.TryGetValue(key, out var val))
        {
            value = val;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public bool ContainsValue(TValue value, IEqualityComparer<TValue>? comparer = null)
    {
        if (_source == null)
        {
            return false;
        }

        comparer ??= EqualityComparer<TValue>.Default;
        foreach (var v in _source.Values)
        {
            if (comparer.Equals(v, value))
            {
                return true;
            }
        }

        return false;
    }
}
