using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Data;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

internal class ClassDataSources
{
    private ClassDataSources()
    {
    }

    public static readonly GetOnlyDictionary<string, ClassDataSources> SourcesPerSession = new();

    public static ClassDataSources Get(string sessionId) => SourcesPerSession.GetOrAdd(sessionId, _ => new());

    public async Task<(T, SharedType, string)> GetItemForIndexAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(int index, Type testClassType, SharedType[] sharedTypes, string[] keys, DataGeneratorMetadata dataGeneratorMetadata, DataSourceContext? dataSourceContext = null) where T : new()
    {
        var shared = sharedTypes.ElementAtOrDefault(index);
        var key = shared == SharedType.Keyed ? GetKey(index, sharedTypes, keys) : string.Empty;

        return
        (
            await GetAsync<T>(shared, testClassType, key, dataGeneratorMetadata, dataSourceContext).ConfigureAwait(false),
            shared,
            key
        );
    }

    private string GetKey(int index, SharedType[] sharedTypes, string[] keys)
    {
        var keyedIndex = sharedTypes.Take(index + 1).Count(x => x == SharedType.Keyed) - 1;

        return keys.ElementAtOrDefault(keyedIndex) ?? throw new ArgumentException($"Key at index {keyedIndex} not found");
    }

    public async Task<T> GetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(SharedType sharedType, Type testClassType, string key, DataGeneratorMetadata dataGeneratorMetadata, DataSourceContext? dataSourceContext = null)
    {
#pragma warning disable CS8603 // Possible null reference return.
        if (sharedType == SharedType.None)
        {
            return await CreateAsync<T>(dataGeneratorMetadata, dataSourceContext);
        }

        if (sharedType == SharedType.PerTestSession)
        {
            return (T)(await TestDataContainer.GetGlobalInstanceAsync(typeof(T), () => CreateAsync(typeof(T), dataGeneratorMetadata, dataSourceContext)).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.PerClass)
        {
            return (T)(await TestDataContainer.GetInstanceForClassAsync(testClassType, typeof(T), () => CreateAsync(typeof(T), dataGeneratorMetadata, dataSourceContext)).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.Keyed)
        {
            return (T)(await TestDataContainer.GetInstanceForKeyAsync(key, typeof(T), () => CreateAsync(typeof(T), dataGeneratorMetadata, dataSourceContext)).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.PerAssembly)
        {
            return (T)(await TestDataContainer.GetInstanceForAssemblyAsync(testClassType.Assembly, typeof(T), () => CreateAsync(typeof(T), dataGeneratorMetadata, dataSourceContext)).ConfigureAwait(false))!;
        }
#pragma warning restore CS8603 // Possible null reference return.

        throw new ArgumentOutOfRangeException();
    }

    public async Task<object> GetAsync(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, Type testClassType, string? key, DataGeneratorMetadata dataGeneratorMetadata, DataSourceContext? dataSourceContext = null)
    {
        if (sharedType == SharedType.None)
        {
            return await CreateAsync(type, dataGeneratorMetadata, dataSourceContext);
        }

        if (sharedType == SharedType.PerTestSession)
        {
            return await TestDataContainer.GetGlobalInstanceAsync(type, () => CreateAsync(type, dataGeneratorMetadata, dataSourceContext)).ConfigureAwait(false);
        }

        if (sharedType == SharedType.PerClass)
        {
            return await TestDataContainer.GetInstanceForClassAsync(testClassType, type, () => CreateAsync(type, dataGeneratorMetadata, dataSourceContext)).ConfigureAwait(false);
        }

        if (sharedType == SharedType.Keyed)
        {
            return await TestDataContainer.GetInstanceForKeyAsync(key!, type, () => CreateAsync(type, dataGeneratorMetadata, dataSourceContext)).ConfigureAwait(false);
        }

        if (sharedType == SharedType.PerAssembly)
        {
            return await TestDataContainer.GetInstanceForAssemblyAsync(testClassType.Assembly, type, () => CreateAsync(type, dataGeneratorMetadata, dataSourceContext)).ConfigureAwait(false);
        }

        throw new ArgumentOutOfRangeException();
    }

    [return: NotNull]
    private static async Task<T> CreateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(DataGeneratorMetadata dataGeneratorMetadata, DataSourceContext? dataSourceContext)
    {
        return ((T)(await CreateAsync(typeof(T), dataGeneratorMetadata, dataSourceContext)))!;
    }

    private static async Task<object> CreateAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, DataGeneratorMetadata dataGeneratorMetadata, DataSourceContext? dataSourceContext)
    {
        try
        {
            var instance = Activator.CreateInstance(type)!;
            
            // Use ambient context if no explicit context is provided
            var contextToUse = dataSourceContext ?? DataSourceExecutionContext.Current;
            
            // Initialize the instance with dependency tracking if context is available
            if (contextToUse != null)
            {
                await DataSourceInitializer.InitializeAsync(instance, dataGeneratorMetadata, null, contextToUse).ConfigureAwait(false);
            }
            
            return instance;
        }
        catch (TargetInvocationException targetInvocationException)
        {
            if (targetInvocationException.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(targetInvocationException.InnerException).Throw();
            }

            throw;
        }
    }


    public (T, SharedType, string) GetItemForIndex<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(int index, Type testClassType, SharedType[] sharedTypes, string[] keys, DataGeneratorMetadata dataGeneratorMetadata, DataSourceContext? dataSourceContext = null) where T : new()
    {
        return AsyncToSyncHelper.RunSync(() => GetItemForIndexAsync<T>(index, testClassType, sharedTypes, keys, dataGeneratorMetadata, dataSourceContext));
    }

    public T Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(SharedType sharedType, Type testClassType, string key, DataGeneratorMetadata dataGeneratorMetadata, DataSourceContext? dataSourceContext = null)
    {
        return AsyncToSyncHelper.RunSync(() => GetAsync<T>(sharedType, testClassType, key, dataGeneratorMetadata, dataSourceContext));
    }

    public object Get(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, Type testClassType, string? key, DataGeneratorMetadata dataGeneratorMetadata, DataSourceContext? dataSourceContext = null)
    {
        return AsyncToSyncHelper.RunSync(() => GetAsync(sharedType, type, testClassType, key, dataGeneratorMetadata, dataSourceContext));
    }
}
