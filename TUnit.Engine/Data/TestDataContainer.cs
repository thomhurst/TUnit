using System.Collections.Concurrent;
using TUnit.Engine.Models;

namespace TUnit.Engine.Data;

public static class TestDataContainer
{
    public static readonly GetOnlyDictionary<DictionaryTypeTypeKey, object> InjectedSharedPerClassType = new();
    public static readonly GetOnlyDictionary<Type, object> InjectedSharedGlobally = new();
    public static readonly GetOnlyDictionary<DictionaryStringTypeKey, object> InjectedSharedPerKey = new();
}

public class GetOnlyDictionary<TKey, TValue> where TKey : notnull
{
    internal ConcurrentDictionary<TKey, TValue> InnerDictionary { get; } = new();

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func) => InnerDictionary.GetOrAdd(key, func);
}