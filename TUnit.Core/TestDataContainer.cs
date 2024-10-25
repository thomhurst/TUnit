using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Data;
using TUnit.Core.Helpers;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class TestDataContainer
{
    private static readonly GetOnlyDictionary<Type, object> InjectedSharedGlobally = new();
    private static readonly GetOnlyDictionary<Type, GetOnlyDictionary<Type, object>> InjectedSharedPerClassType = new();
    private static readonly GetOnlyDictionary<Assembly, GetOnlyDictionary<Type, object>> InjectedSharedPerAssembly = new();
    private static readonly GetOnlyDictionary<Type, GetOnlyDictionary<string, object>> InjectedSharedPerKey = new();

#if NET9_0_OR_GREATER
    private static readonly Lock Lock = new();
#else
    private static readonly object Lock = new();
#endif
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, int>> CountsPerKey = new();
    private static readonly ConcurrentDictionary<Type, int> CountsPerGlobalType = new();

    private static Disposer Disposer => new(GlobalContext.Current.GlobalLogger);
    
    public static T GetInstanceForType<T>(Type key, Func<T> func)
    {
        var objectsForClass = InjectedSharedPerClassType.GetOrAdd(key, _ => new GetOnlyDictionary<Type, object>());
        return  (T)objectsForClass.GetOrAdd(typeof(T), _ => func()!);
    }
    
    public static T GetInstanceForAssembly<T>(Assembly assembly, Func<T> func)
    {
        var objectsForClass = InjectedSharedPerAssembly.GetOrAdd(assembly, _ => new GetOnlyDictionary<Type, object>());
        return  (T)objectsForClass.GetOrAdd(typeof(T), _ => func()!);
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
        var instancesForType = InjectedSharedPerKey.GetOrAdd(typeof(T), _ => new GetOnlyDictionary<string, object>());

        return  (T)instancesForType.GetOrAdd(key, _ => func()!);
    }
    
    internal static async ValueTask ConsumeKey(string key, Type type)
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

    internal static async ValueTask ConsumeGlobalCount(Type type)
    {
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