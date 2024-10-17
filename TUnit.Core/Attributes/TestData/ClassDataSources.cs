using System.Reflection;
using TUnit.Core.Data;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

internal class ClassDataSources
{
    public static GetOnlyDictionary<Type, Task> GlobalInitializers = new();
    public static readonly GetOnlyDictionary<Type, GetOnlyDictionary<Type, Task>> TestClassTypeInitializers = new();
    public static readonly GetOnlyDictionary<Type, GetOnlyDictionary<Assembly, Task>> AssemblyInitializers = new();
    public static readonly GetOnlyDictionary<Type, GetOnlyDictionary<string, Task>> KeyedInitializers = new();
    
    public static Task InitializeObject(object? item)
    {
        if (item is IAsyncInitializer asyncInitializer)
        {
            return asyncInitializer.InitializeAsync();
        }
        
        return Task.CompletedTask;
    }
    
    public static async ValueTask OnTestRegistered<T>(TestContext testContext, bool isStatic, SharedType shared, string key, T? item)
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

    public static async ValueTask OnTestStart<T>(BeforeTestContext beforeTestContext, bool isStatic, SharedType shared, string key, T? item)
    {
        if (isStatic)
        {
            // Done already before test start
            return;
        }
        
        await Initialize(beforeTestContext.TestContext, shared, key, item);
    }

    public static Task Initialize<T>(TestContext testContext, SharedType shared, string key, T? item)
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

    public static async ValueTask OnTestEnd<T>(SharedType shared, string key, T? item)
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

    public static async ValueTask IfLastTestInClass<T>(SharedType shared)
    {
        if (shared == SharedType.ForClass)
        {
            await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(TestDataContainer.GetInstanceForType(typeof(T), () => default(T)!));
        }
    }

    public static async ValueTask IfLastTestInAssembly<T>(SharedType shared)
    {
        if (shared == SharedType.ForAssembly)
        {
            await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(TestDataContainer.GetInstanceForType(typeof(T), () => default(T)!));
        }
    }
}