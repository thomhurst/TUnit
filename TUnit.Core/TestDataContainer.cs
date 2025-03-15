using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Data;
using TUnit.Core.Helpers;

namespace TUnit.Core;

/// <summary>
/// Represents a container for test data.
/// </summary>
internal static class TestDataContainer
{
    private static readonly GetOnlyDictionary<Type, object> InjectedSharedGlobally = new();
    private static readonly GetOnlyDictionary<Type, GetOnlyDictionary<Type, object>> InjectedSharedPerClassType = new();
    private static readonly GetOnlyDictionary<Assembly, GetOnlyDictionary<Type, object>> InjectedSharedPerAssembly = new();
    private static readonly GetOnlyDictionary<Type, GetOnlyDictionary<string, object>> InjectedSharedPerKey = new();

    private static readonly Lock Lock = new();
    
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Counter>> CountsPerKey = new();
    private static readonly ConcurrentDictionary<Type, Counter> CountsPerTestSession = new();
    private static readonly ConcurrentDictionary<Assembly, ConcurrentDictionary<Type, Counter>> CountsPerAssembly = new();
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Counter>> CountsPerTestClass = new();

    private static Disposer Disposer => new(GlobalContext.Current.GlobalLogger);
    
    /// <summary>
    /// Gets an instance for the specified class.
    /// </summary>
    /// <typeparam name="T">The type of the instance.</typeparam>
    /// <param name="testClass">The test class type.</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static T GetInstanceForClass<T>(Type testClass, Func<T> func)
    {
        var objectsForClass = InjectedSharedPerClassType.GetOrAdd(testClass, _ => new GetOnlyDictionary<Type, object>());
        
        return (T)objectsForClass.GetOrAdd(typeof(T), _ => func()!);
    }
    
    /// <summary>
    /// Gets an instance for the specified assembly.
    /// </summary>
    /// <typeparam name="T">The type of the instance.</typeparam>
    /// <param name="assembly">The assembly.</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static T GetInstanceForAssembly<T>(Assembly assembly, Func<T> func)
    {
        var objectsForClass = InjectedSharedPerAssembly.GetOrAdd(assembly, _ => new GetOnlyDictionary<Type, object>());
        
        return  (T)objectsForClass.GetOrAdd(typeof(T), _ => func()!);
    }
    
    /// <summary>
    /// Increments the global usage count for the specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    public static void IncrementGlobalUsage(Type type)
    {
        CountsPerTestSession.GetOrAdd(type, _ => new Counter()).Increment();
    }
    
    /// <summary>
    /// Gets a global instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the instance.</typeparam>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static T GetGlobalInstance<T>(Func<T> func)
    {
        return (T)InjectedSharedGlobally.GetOrAdd(typeof(T), _ => func()!);
    }
    
    /// <summary>
    /// Increments the usage count for the specified test class and type.
    /// </summary>
    /// <param name="testClassType">The test class type.</param>
    /// <param name="type">The type.</param>
    public static void IncrementTestClassUsage(Type testClassType, Type type)
    {
        var itemsForTestClass = CountsPerTestClass.GetOrAdd(testClassType, _ => []);

        itemsForTestClass.GetOrAdd(type, _ => new Counter()).Increment();
    }
    
    /// <summary>
    /// Increments the usage count for the specified assembly and type.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="type">The type.</param>
    public static void IncrementAssemblyUsage(Assembly assembly, Type type)
    {
        var itemsForAssembly = CountsPerAssembly.GetOrAdd(assembly, _ => []);

        itemsForAssembly.GetOrAdd(type, _ => new Counter()).Increment();
    }

    /// <summary>
    /// Increments the usage count for the specified key and type.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type.</param>
    public static void IncrementKeyUsage(string key, Type type)
    {
        var keysForType = CountsPerKey.GetOrAdd(type, _ => []);

        keysForType.GetOrAdd(key, _ => new Counter()).Increment();
    }

    /// <summary>
    /// Gets an instance for the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the instance.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="func">The function to create the instance.</param>
    /// <returns>The instance.</returns>
    public static T GetInstanceForKey<T>(string key, Func<T> func)
    {
        var instancesForType = InjectedSharedPerKey.GetOrAdd(typeof(T), _ => new GetOnlyDictionary<string, object>());

        return (T)instancesForType.GetOrAdd(key, _ => func()!);
    }
    
    /// <summary>
    /// Consumes the count for the specified key and type.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type.</param>
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

    /// <summary>
    /// Consumes the global count for the specified item.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="item">The item.</param>
    internal static async ValueTask ConsumeGlobalCount<T>(T? item)
    {
        if (CountsPerTestSession[typeof(T)].Decrement() > 0)
        {
            return;
        }
        
        await Disposer.DisposeAsync(item);
    }
    
    /// <summary>
    /// Consumes the assembly count for the specified item.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="assembly">The assembly.</param>
    /// <param name="item">The item.</param>
    internal static async ValueTask ConsumeAssemblyCount<T>(Assembly assembly, T? item)
    {
        if (CountsPerAssembly[assembly][typeof(T)].Decrement() > 0)
        {
            return;
        }
        
        await Disposer.DisposeAsync(item);
    }
    
    /// <summary>
    /// Consumes the test class count for the specified item.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="testClassType">The test class type.</param>
    /// <param name="item">The item.</param>
    internal static async ValueTask ConsumeTestClassCount<T>(Type testClassType, T? item)
    {
        if (CountsPerTestClass[testClassType][typeof(T)].Decrement() > 0)
        {
            return;
        }
        
        await Disposer.DisposeAsync(item);
    }
}