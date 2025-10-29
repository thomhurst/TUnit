using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Data;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class ThreadSafeDictionary<TKey,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _innerDictionary = new();

    public ICollection<TKey> Keys => _innerDictionary.Keys;

    public IEnumerable<TValue> Values => _innerDictionary.Values.Select(static lazy => lazy.Value);

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
    {
        var lazy = _innerDictionary.GetOrAdd(key,
            k => new Lazy<TValue>(() => func(k), LazyThreadSafetyMode.ExecutionAndPublication));

        return lazy.Value;
    }

    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        if (_innerDictionary.TryGetValue(key, out var lazy))
        {
            value = lazy.Value!;
            return true;
        }

        value = default!;
        return false;
    }

    public TValue? Remove(TKey key)
    {
        if (_innerDictionary.TryRemove(key, out var lazy))
        {
            return lazy.Value;
        }

        return default(TValue?);
    }

    public TValue this[TKey key] => _innerDictionary.TryGetValue(key, out var lazy)
        ? lazy.Value
        : throw new KeyNotFoundException($"Key '{key}' not found in dictionary");
}
