using System.Collections.Concurrent;

namespace TUnit.Engine.Data;

#if !DEBUG
using System.ComponentModel;
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public class GetOnlyDictionary<TKey, TValue> where TKey : notnull
{
    internal ConcurrentDictionary<TKey, TValue> InnerDictionary { get; } = new();

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func) => InnerDictionary.GetOrAdd(key, func);
}