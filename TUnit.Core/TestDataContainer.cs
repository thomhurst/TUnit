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

    private static readonly Lock Lock = new();
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Counter>> CountsPerKey = new();
    private static readonly ConcurrentDictionary<Type, Counter> CountsPerGlobalType = new();

    private static Disposer Disposer => new(GlobalContext.Current.GlobalLogger);
    
    public static T GetInstanceForClass<T>(Type testClass, Func<T> func, DataGeneratorMetadata dataGeneratorMetadata)
    {
        var objectsForClass = InjectedSharedPerClassType.GetOrAdd(testClass, _ => new GetOnlyDictionary<Type, object>());
        return (T)objectsForClass.GetOrAdd(typeof(T), _ =>
        {
            var t = func()!;

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInClass += async (_, _) =>
                await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(t);
            
            return t;
        });
    }
    
    public static T GetInstanceForAssembly<T>(Assembly assembly, Func<T> func,
        DataGeneratorMetadata dataGeneratorMetadata)
    {
        var objectsForClass = InjectedSharedPerAssembly.GetOrAdd(assembly, _ => new GetOnlyDictionary<Type, object>());
        
        return  (T)objectsForClass.GetOrAdd(typeof(T), _ =>
        {
            var t = func()!;

            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnLastTestInAssembly += async (_, _) =>
                await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(t);
            
            return t;
        });
    }
    
    public static void IncrementGlobalUsage(Type type)
    {
        CountsPerGlobalType.GetOrAdd(type, _ => new Counter()).Increment();
    }
    
    public static T GetGlobalInstance<T>(Func<T> func, DataGeneratorMetadata dataGeneratorMetadata)
    {
        return (T)InjectedSharedGlobally.GetOrAdd(typeof(T), _ => func()!);
    }

    public static void IncrementKeyUsage(string key, Type type)
    {
        var keysForType = CountsPerKey.GetOrAdd(type, _ => []);

        keysForType.GetOrAdd(key, _ => new Counter()).Increment();
    }

    public static T GetInstanceForKey<T>(string key, Func<T> func)
    {
        var instancesForType = InjectedSharedPerKey.GetOrAdd(typeof(T), _ => new GetOnlyDictionary<string, object>());

        return (T)instancesForType.GetOrAdd(key, _ => func()!);
    }
    
    internal static async ValueTask ConsumeKey(string key, Type type)
    {
        var keysForType = CountsPerKey[type];
            
        if (keysForType[key].Decrement() > 0)
        {
            return;
        }
        
        var instancesForType = InjectedSharedPerKey.GetOrAdd(type, _ => new GetOnlyDictionary<string, object>());
        
        await Disposer.DisposeAsync(instancesForType.Remove(key));
    }

    internal static async ValueTask ConsumeGlobalCount(Type type)
    {
        if (CountsPerGlobalType[type].Decrement() > 0)
        {
            return;
        }
        
        await Disposer.DisposeAsync(InjectedSharedGlobally.Remove(type));
    }
}