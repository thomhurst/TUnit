using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace TUnit.Core.Data;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class ThreadSafeDictionary<TKey, 
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue> 
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _values = new();
    private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _lazyFactories = new();

    public ICollection<TKey> Keys => _values.Keys;

    public ICollection<TValue> Values => _values.Values;

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
    {
        if (_values.TryGetValue(key, out var existingValue))
        {
            return existingValue;
        }

        var lazy = _lazyFactories.GetOrAdd(key, 
            k => new Lazy<TValue>(() => func(k), LazyThreadSafetyMode.ExecutionAndPublication));
        
        var value = lazy.Value;
        
        _values.TryAdd(key, value);
        _lazyFactories.TryRemove(key, out _);
        
        return value;
    }

    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        if (_values.TryGetValue(key, out value!))
        {
            return true;
        }
        
        if (_lazyFactories.TryGetValue(key, out var lazy))
        {
            value = lazy.Value!;
            _values.TryAdd(key, value);
            _lazyFactories.TryRemove(key, out _);
            return true;
        }
        
        value = default!;
        return false;
    }

    public TValue? Remove(TKey key)
    {
        _lazyFactories.TryRemove(key, out _);
        
        if (_values.TryRemove(key, out var value))
        {
            return value;
        }

        return default(TValue?);
    }

    public TValue this[TKey key]
    {
        get
        {
            if (_values.TryGetValue(key, out var value))
            {
                return value;
            }
            
            if (_lazyFactories.TryGetValue(key, out var lazy))
            {
                var lazyValue = lazy.Value;
                _values.TryAdd(key, lazyValue);
                _lazyFactories.TryRemove(key, out _);
                return lazyValue;
            }
            
            throw new KeyNotFoundException($"Key '{key}' not found in dictionary");
        }
    }
}
