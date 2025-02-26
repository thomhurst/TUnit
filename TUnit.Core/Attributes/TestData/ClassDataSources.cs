using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Data;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
#pragma warning disable CS0618 // Type or member is obsolete

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
    
    public (T, SharedType, string) GetItemForIndex<T>(int index, Type testClassType, SharedType[] sharedTypes, string[] keys, DataGeneratorMetadata dataGeneratorMetadata) where T : new()
    {
        var shared = sharedTypes.ElementAtOrDefault(index);
        var key = shared == SharedType.Keyed ? GetKey(index, sharedTypes, keys) : string.Empty;

        return
        (
            Get<T>(shared, testClassType, key, dataGeneratorMetadata),
            shared,
            key
        );
    }

    private string GetKey(int index, SharedType[] sharedTypes, string[] keys)
    {
        var keyedIndex = sharedTypes.Take(index + 1).Count(x => x == SharedType.Keyed) - 1;

        return keys.ElementAtOrDefault(keyedIndex) ?? throw new ArgumentException($"Key at index {keyedIndex} not found");
    }
    
    public T Get<T>(SharedType sharedType, Type testClassType, string key, DataGeneratorMetadata dataGeneratorMetadata) where T : new()
    {
        if (sharedType == SharedType.None)
        {
            return Create<T>();
        }

        if (sharedType == SharedType.PerTestSession)
        {
            return TestDataContainer.GetGlobalInstance(Create<T>, dataGeneratorMetadata);
        }

        if (sharedType == SharedType.PerClass)
        {
            return TestDataContainer.GetInstanceForClass(testClassType, Create<T>, dataGeneratorMetadata);
        }

        if (sharedType == SharedType.Keyed)
        {
            return TestDataContainer.GetInstanceForKey(key, Create<T>);
        }

        if (sharedType == SharedType.PerAssembly)
        {
            return TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, Create<T>, dataGeneratorMetadata);
        }

        throw new ArgumentOutOfRangeException();
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
            case SharedType.PerClass:
            case SharedType.PerAssembly:
                break;
            case SharedType.PerTestSession:
                TestDataContainer.IncrementGlobalUsage(typeof(T));
                break;
            case SharedType.Keyed:
                TestDataContainer.IncrementKeyUsage(key, typeof(T));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (isStatic)
        {
            await Initialize(testContext, shared, key, item);
        }
    }

    public async ValueTask OnInitialize<T>(TestContext testContext, bool isStatic, SharedType shared, string key, T? item)
    {
        if (isStatic)
        {
            // Done already before test start
            return;
        }
        
        await Initialize(testContext, shared, key, item);
    }

    public Task Initialize<T>(TestContext testContext, SharedType shared, string key, T? item)
    {
        if (shared == SharedType.PerTestSession)
        {
            return GlobalInitializers.GetOrAdd(typeof(T), _ => InitializeObject(item));
        }

        if (shared == SharedType.None)
        {
            return InitializeObject(item);
        }

        if (shared == SharedType.PerClass)
        {
            var innerDictionary = TestClassTypeInitializers.GetOrAdd(typeof(T),
                _ => new GetOnlyDictionary<Type, Task>());
            
            return innerDictionary.GetOrAdd(testContext.TestDetails.TestClass.Type,
                _ => InitializeObject(item));
        }

        if (shared == SharedType.PerAssembly)
        {
            var innerDictionary = AssemblyInitializers.GetOrAdd(typeof(T),
                _ => new GetOnlyDictionary<Assembly, Task>());
            
            return innerDictionary.GetOrAdd(testContext.TestDetails.TestClass.Type.Assembly,
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

    public async ValueTask OnDispose<T>(SharedType shared, string key, T? item)
    {
        if (shared is SharedType.None)
        {
            await new Disposer(GlobalContext.Current.GlobalLogger).DisposeAsync(item);
        }
        
        if (shared == SharedType.Keyed)
        {
            await TestDataContainer.ConsumeKey(key, typeof(T));
        }

        if (shared == SharedType.PerTestSession)
        {
            await TestDataContainer.ConsumeGlobalCount(typeof(T));
        }
    }
    
    public static bool IsStaticProperty(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return dataGeneratorMetadata.MembersToGenerate is [SourceGeneratedPropertyInformation { IsStatic: true }];
    }

    private static T Create<T>() where T : new()
    {
        try
        {
            return new T();
        }
        catch (TargetInvocationException targetInvocationException)
        {
            if (targetInvocationException.InnerException != null)
            {
#if NET
                ExceptionDispatchInfo.Throw(targetInvocationException.InnerException);
#else
                ExceptionDispatchInfo.Capture(targetInvocationException.InnerException).Throw();
#endif
            }

            throw;
        }
    }
}