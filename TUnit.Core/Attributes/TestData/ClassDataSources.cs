using System.Reflection;
using TUnit.Core.Data;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

internal class ClassDataSources
{
    private ClassDataSources()
    {
    }
    
    public GetOnlyDictionary<Type, Task> GlobalInitializers = new();
    public readonly GetOnlyDictionary<Type, GetOnlyDictionary<Type, Task>> TestClassTypeInitializers = new();
    public readonly GetOnlyDictionary<Type, GetOnlyDictionary<Assembly, Task>> AssemblyInitializers = new();
    public readonly GetOnlyDictionary<Type, GetOnlyDictionary<string, Task>> KeyedInitializers = new();

    public static readonly GetOnlyDictionary<string, ClassDataSources> SourcesPerSession = new();

    public static ClassDataSources Get(string sessionId) => SourcesPerSession.GetOrAdd(sessionId, _ => new());
    
    public (T, SharedType, string) GetItemForIndex<T>(int index, Type testClassType, SharedType[] sharedTypes, string[] keys) where T : new()
    {
        var shared = sharedTypes.ElementAtOrDefault(index);
        var key = shared == SharedType.Keyed ? GetKey(index, sharedTypes, keys) : string.Empty;

        return
        (
            Get<T>(shared, testClassType, key),
            shared,
            key
        );
    }

    private string GetKey(int index, SharedType[] sharedTypes, string[] keys)
    {
        var keyedIndex = sharedTypes.Take(index + 1).Count(x => x == SharedType.Keyed) - 1;

        return keys.ElementAtOrDefault(keyedIndex) ?? throw new ArgumentException($"Key at index {keyedIndex} not found");
    }
    
    public T Get<T>(SharedType sharedType, Type testClassType, string key) where T : new()
    {
        return sharedType switch
        {
            SharedType.None => new T(),
            SharedType.Globally => TestDataContainer.GetGlobalInstance(() => new T()),
            SharedType.ForClass => TestDataContainer.GetInstanceForType(testClassType, () => new T()),
            SharedType.Keyed => TestDataContainer.GetInstanceForKey(key, () => new T()),
            SharedType.ForAssembly => TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, () => new T()),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public Task InitializeObject(object? item)
    {
        if (item is IAsyncInitializer asyncInitializer)
        {
            return asyncInitializer.InitializeAsync();
        }
        
        return Task.CompletedTask;
    }
    
    public async ValueTask OnTestRegistered<T>(TestContext testContext, bool isStatic, SharedType shared, string key, T? item)
    {
        switch (shared)
        {
            case SharedType.None:
                break;
            case SharedType.ForClass:
                break;
            case SharedType.Globally:
                TestDataContainer.IncrementGlobalUsage(typeof(T));
                break;
            case SharedType.Keyed:
                TestDataContainer.IncrementKeyUsage(key, typeof(T));
                break;
            case SharedType.ForAssembly:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (isStatic)
        {
            await Initialize(testContext, shared, key, item);
        }
    }

    public async ValueTask OnTestStart<T>(BeforeTestContext beforeTestContext, bool isStatic, SharedType shared, string key, T? item)
    {
        if (isStatic)
        {
            // Done already before test start
            return;
        }
        
        await Initialize(beforeTestContext.TestContext, shared, key, item);
    }

    public Task Initialize<T>(TestContext testContext, SharedType shared, string key, T? item)
    {
        if (shared == SharedType.Globally)
        {
            return GlobalInitializers.GetOrAdd(typeof(T), _ => InitializeObject(item));
        }

        if (shared == SharedType.None)
        {
            return InitializeObject(item);
        }

        if (shared == SharedType.ForClass)
        {
            var innerDictionary = TestClassTypeInitializers.GetOrAdd(typeof(T),
                _ => new GetOnlyDictionary<Type, Task>());
            
            return innerDictionary.GetOrAdd(testContext.TestDetails.ClassType,
                _ => InitializeObject(item));
        }

        if (shared == SharedType.ForAssembly)
        {
            var innerDictionary = AssemblyInitializers.GetOrAdd(typeof(T),
                _ => new GetOnlyDictionary<Assembly, Task>());
            
            return innerDictionary.GetOrAdd(testContext.TestDetails.ClassType.Assembly,
                _ => InitializeObject(item));
        }

        if (shared == SharedType.Keyed)
        {
            var innerDictionary = KeyedInitializers.GetOrAdd(typeof(T),
                _ => new GetOnlyDictionary<string, Task>());
            
            return innerDictionary.GetOrAdd(key, _ => InitializeObject(item));
        }

        throw new ArgumentOutOfRangeException(nameof(shared));
    }

    public async ValueTask OnTestEnd<T>(SharedType shared, string key, T? item)
    {
        if (shared == SharedType.None)
        {
            await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(item);
        }

        if (shared == SharedType.Keyed)
        {
            await TestDataContainer.ConsumeKey(key, typeof(T));
        }

        if (shared == SharedType.Globally)
        {
            await TestDataContainer.ConsumeGlobalCount(typeof(T));
        }
    }

    public async ValueTask IfLastTestInClass<T>(SharedType shared)
    {
        if (shared == SharedType.ForClass)
        {
            await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(TestDataContainer.GetInstanceForType(typeof(T), () => default(T)!));
        }
    }

    public async ValueTask IfLastTestInAssembly<T>(SharedType shared)
    {
        if (shared == SharedType.ForAssembly)
        {
            await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(TestDataContainer.GetInstanceForType(typeof(T), () => default(T)!));
        }
    }
}