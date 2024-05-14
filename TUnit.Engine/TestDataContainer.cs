using System.Collections.Concurrent;
using TUnit.Engine.Models;

namespace TUnit.Engine;

public static class TestDataContainer
{
    public static readonly ConcurrentDictionary<DictionaryTypeTypeKey, object> InjectedSharedPerClassType = new();
    public static readonly ConcurrentDictionary<Type, object> InjectedSharedGlobally = new();
    public static readonly ConcurrentDictionary<DictionaryStringTypeKey, object> InjectedSharedPerKey = new();
}