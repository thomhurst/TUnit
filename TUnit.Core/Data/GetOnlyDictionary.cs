using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Data;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class GetOnlyDictionary<TKey, 
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue> 
    where TKey : notnull
{
    // Using Lazy<TValue> ensures factory functions are only executed once per key,
    // solving the race condition issue with ConcurrentDictionary.GetOrAdd
    private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _innerDictionary = new();

    public ICollection<TKey> Keys => _innerDictionary.Keys;

    public ICollection<TValue> Values => _innerDictionary.Values.Select(lazy => lazy.Value).ToList();

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
    {
        // The Lazy wrapper ensures the factory function is only executed once,
        // even if multiple threads race to add the same key
        // We use ExecutionAndPublication mode for thread safety
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
