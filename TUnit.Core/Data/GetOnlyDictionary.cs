using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Data;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class GetOnlyDictionary<TKey, TValue> where TKey : notnull
{
    private ConcurrentDictionary<TKey, TValue> InnerDictionary { get; } = new();

    // ReSharper disable once StaticMemberInGenericType
    private static readonly Lock _lock = new();

    public ICollection<TKey> Keys
    {
        get
        {
            lock (_lock)
            {
                return InnerDictionary.Keys;
            }
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            lock (_lock)
            {
                return InnerDictionary.Values;
            }
        }
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
    {
        lock (_lock)
        {
            return InnerDictionary.GetOrAdd(key, func);
        }
    }

    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        lock (_lock)
        {
            return InnerDictionary.TryGetValue(key, out value!);
        }
    }

    public TValue? Remove(TKey key)
    {
        lock (_lock)
        {
            if (InnerDictionary.TryRemove(key, out var value))
            {
                return value;
            }

            return default(TValue?);
        }
    }

    public TValue this[TKey key] => InnerDictionary[key];
}
