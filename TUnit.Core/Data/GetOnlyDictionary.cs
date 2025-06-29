using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Data;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class GetOnlyDictionary<TKey, TValue> where TKey : notnull
{
    private ConcurrentDictionary<TKey, TValue> InnerDictionary { get; } = new();

    private static readonly Lock Lock = new();

    public ICollection<TKey> Keys => InnerDictionary.Keys;
    public ICollection<TValue> Values => InnerDictionary.Values;

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
    {
        lock (Lock)
        {
            return InnerDictionary.GetOrAdd(key, func);
        }
    }

    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        lock (Lock)
        {
            return InnerDictionary.TryGetValue(key, out value!);
        }
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func, out bool previouslyExisted)
    {
        lock (Lock)
        {
            if (InnerDictionary.TryGetValue(key, out var foundValue))
            {
                previouslyExisted = true;
                return foundValue;
            }

            previouslyExisted = false;
            return InnerDictionary.GetOrAdd(key, func);
        }
    }

    public TValue? Remove(TKey key)
    {
        lock (Lock)
        {
            if (InnerDictionary.TryRemove(key, out var value))
            {
                return value;
            }

            return default;
        }
    }

    public TValue this[TKey key] => InnerDictionary[key];
}
