using System.Collections.Concurrent;

namespace TUnit.Core.Data;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class GetOnlyDictionary<TKey, TValue> where TKey : notnull
{
    private ConcurrentDictionary<TKey, TValue> InnerDictionary { get; } = new();

#if NET9_0_OR_GREATER
    private static readonly Lock Lock = new();
#else
    private static readonly object Lock = new();
#endif
    
    public ICollection<TKey> Keys => InnerDictionary.Keys;
    public ICollection<TValue> Values => InnerDictionary.Values;

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
    {
        lock (Lock)
        {
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
}