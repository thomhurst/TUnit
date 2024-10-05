using System.Collections.Concurrent;
using TUnit.Core.Data;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class TestDataContainer
{
    private static readonly GetOnlyDictionary<Type, object> InjectedSharedGlobally = new();
    private static readonly GetOnlyDictionary<Type, GetOnlyDictionary<Type, object>> InjectedSharedPerClassType = new();
    private static readonly GetOnlyDictionary<Type, GetOnlyDictionary<string, object>> InjectedSharedPerKey = new();

    internal static readonly Dictionary<Type, Lazy<Task>> InjectedSharedGloballyInitializations = new();
    private static readonly GetOnlyDictionary<Type, Dictionary<Type, Lazy<Task>>> InjectedSharedPerClassTypeInitializations = new();
    private static readonly GetOnlyDictionary<Type, Dictionary<string, Lazy<Task>>> InjectedSharedPerKeyInitializations = new();
    
    private static readonly object Lock = new();
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, int>> CountsPerKey = new();
    private static readonly ConcurrentDictionary<Type, int> CountsPerGlobalType = new();

    private static Disposer Disposer => new(GlobalContext.Current.GlobalLogger);
    
    public static T GetInstanceForType<T>(Type key, Func<T> func) where T : class
    {
        var objectsForClass = InjectedSharedPerClassType.GetOrAdd(key, _ => new GetOnlyDictionary<Type, object>());
        return  (T)objectsForClass.GetOrAdd(typeof(T), _ =>
        {
            var obj = func();

            if (obj is IAsyncInitializer asyncInitializer)
            {
                var dictionary =
                    InjectedSharedPerClassTypeInitializations.GetOrAdd(key, _ => new Dictionary<Type, Lazy<Task>>());
                dictionary.Add(typeof(T), new Lazy<Task>(() => asyncInitializer.InitializeAsync()));
            }

            return obj;
        });
    }
    
    public static void IncrementGlobalUsage(Type type)
    {
        var count = CountsPerGlobalType.GetOrAdd(type, _ => 0);

        CountsPerGlobalType[type] = count + 1;
    }
    
    public static T GetGlobalInstance<T>(Func<T> func) where T : class
    {
        return (T)InjectedSharedGlobally.GetOrAdd(typeof(T), _ =>
        {
            var obj = func();

            if (obj is IAsyncInitializer asyncInitializer)
            { 
                InjectedSharedGloballyInitializations[typeof(T)] = new Lazy<Task>(() => asyncInitializer.InitializeAsync());
            }

            return obj;
        });
    }

    public static void IncrementKeyUsage(string key, Type type)
    {
        var keysForType = CountsPerKey.GetOrAdd(type, _ => new ConcurrentDictionary<string, int>());

        var count = keysForType.GetOrAdd(key, _ => 0);

        keysForType[key] = count + 1;
    }

    public static T GetInstanceForKey<T>(string key, Func<T> func) where T : class
    {
        var instancesForType = InjectedSharedPerKey.GetOrAdd(typeof(T), _ => new GetOnlyDictionary<string, object>());

        return  (T)instancesForType.GetOrAdd(key, _ =>
        {
            var obj = func();

            if (obj is IAsyncInitializer asyncInitializer)
            {
                var dictionary =
                    InjectedSharedPerKeyInitializations.GetOrAdd(typeof(T), _ => new Dictionary<string, Lazy<Task>>());
                dictionary.Add(key, new Lazy<Task>(() => asyncInitializer.InitializeAsync()));
            }

            return obj;
        });
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

    internal static async Task ConsumeGlobalCount(Type type)
    {
        if (TestDictionary.StaticInjectedProperties.TryGet(type, out var _))
        {
            // This is also being used in static properties, so we'll dispose it after the test session.
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

    internal static Task RunInitializer(Type testClassType, TestData testData)
    {
        if (testData.Argument is not IAsyncInitializer asyncInitializer)
        {
            return Task.CompletedTask;
        }

        return testData.InjectedDataType switch
        {
            InjectedDataType.None => asyncInitializer.InitializeAsync(),
            InjectedDataType.SharedGlobally => InjectedSharedGloballyInitializations[testData.Type].Value,
            InjectedDataType.SharedByTestClassType => InjectedSharedPerClassTypeInitializations[testClassType][
                testData.Type].Value,
            InjectedDataType.SharedByKey => InjectedSharedPerKeyInitializations[testData.Type][testData.StringKey!]
                .Value,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}