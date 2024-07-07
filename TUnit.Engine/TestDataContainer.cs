using System.Collections.Concurrent;
using TUnit.Engine.Data;
using TUnit.Engine.Helpers;
using TUnit.Engine.Logging;

namespace TUnit.Engine;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)]
#endif
public static class TestDataContainer
{
    private static readonly GetOnlyDictionary<Type, object> InjectedSharedGlobally = new();
    private static readonly GetOnlyDictionary<Type, GetOnlyDictionary<Type, object>> InjectedSharedPerClassType = new();
    private static readonly GetOnlyDictionary<Type, GetOnlyDictionary<string, object>> InjectedSharedPerKey = new();

    private static readonly object Lock = new();
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, int>> CountsPerKey = new();
    private static readonly ConcurrentDictionary<Type, int> CountsPerGlobalType = new();
    
    private static Disposer Disposer => new(TUnitLogger.Instance);
    
    public static T GetInstanceForType<T>(Type key, Func<T> func)
    {
        var objectsForClass = InjectedSharedPerClassType.GetOrAdd(key, _ => new GetOnlyDictionary<Type, object>());
        return (T)objectsForClass.GetOrAdd(typeof(T), _ => func()!);
    }

    public static void IncrementGlobalUsage(Type type)
    {
        var count = CountsPerGlobalType.GetOrAdd(type, _ => 0);

        CountsPerGlobalType[type] = count + 1;
    }
    
    public static T GetGlobalInstance<T>(Func<T> func)
    {
        return (T)InjectedSharedGlobally.GetOrAdd(typeof(T), _ => func()!);
    }

    public static void IncrementKeyUsage(string key, Type type)
    {
        var keysForType = CountsPerKey.GetOrAdd(type, _ => new ConcurrentDictionary<string, int>());

        var count = keysForType.GetOrAdd(key, _ => 0);

        keysForType[key] = count + 1;
    }

    public static T GetInstanceForKey<T>(string key, Func<T> func)
    {
        lock (Lock)
        {
            var instancesForType = InjectedSharedPerKey.GetOrAdd(typeof(T), _ => new GetOnlyDictionary<string, object>());

            return (T)instancesForType.GetOrAdd(key, _ => func()!);
        }
    }
    
    internal static async Task OnLastInstance(Type testClassType)
    {
        var typesPerType = InjectedSharedPerClassType.GetOrAdd(testClassType, _ => new GetOnlyDictionary<Type, object>());
        
        foreach (var key in typesPerType.Keys)
        {
            await Disposer.DisposeAsync(typesPerType.Remove(key));
        }
    }
    
    internal static async Task ConsumeKey(string key, Type type)
    {
        lock (Lock)
        {
            var keysForType = CountsPerKey.GetOrAdd(type, _ => new ConcurrentDictionary<string, int>());

            var count = keysForType.GetOrAdd(key, _ => 0);

            var newCount = count - 1;

            keysForType[key] = newCount;

            if (newCount > 0)
            {
                return;
            }
        }
        
        var instancesForType = InjectedSharedPerKey.GetOrAdd(type, _ => new GetOnlyDictionary<string, object>());
        
        await Disposer.DisposeAsync(instancesForType.Remove(key));
    }

    internal static async Task ConsumeGlobalCount(Type? type)
    {
        if (type is null)
        {
            return;
        }

        lock (Lock)
        {
            var count = CountsPerGlobalType.GetOrAdd(type, _ => 0);

            var newCount = count - 1;
            
            CountsPerGlobalType[type] = newCount;
            
            if (newCount > 0)
            {
                return;
            }
        }
        
        await Disposer.DisposeAsync(InjectedSharedGlobally.Remove(type));
    }
}