using System.Collections.Concurrent;

namespace TUnit.Engine.Data;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
#endif
public class GetOnlyDictionary<TKey, TValue> where TKey : notnull
{
    internal ConcurrentDictionary<TKey, TValue> InnerDictionary { get; } = new();

    public ICollection<TKey> Keys => InnerDictionary.Keys;
    public ICollection<TValue> Values => InnerDictionary.Values;

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func) => InnerDictionary.GetOrAdd(key, func);

    public TValue? Remove(TKey key)
    {
        if (InnerDictionary.TryRemove(key, out var value))
        {
            return value;
        }

        return default;
    }
}