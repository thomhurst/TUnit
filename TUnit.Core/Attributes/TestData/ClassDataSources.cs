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

    public async Task<(T, SharedType, string)> GetItemForIndexAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(int index, Type testClassType, SharedType[] sharedTypes, string[] keys, DataGeneratorMetadata dataGeneratorMetadata) where T : new()
    {
        var shared = sharedTypes.ElementAtOrDefault(index);
        var key = shared == SharedType.Keyed ? GetKey(index, sharedTypes, keys) : string.Empty;

        return
        (
            await GetAsync<T>(shared, testClassType, key, dataGeneratorMetadata).ConfigureAwait(false),
            shared,
            key
        );
    }

    private string GetKey(int index, SharedType[] sharedTypes, string[] keys)
    {
        var keyedIndex = sharedTypes.Take(index + 1).Count(x => x == SharedType.Keyed) - 1;

        return keys.ElementAtOrDefault(keyedIndex) ?? throw new ArgumentException($"Key at index {keyedIndex} not found");
    }

    public async Task<T> GetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(SharedType sharedType, Type testClassType, string key, DataGeneratorMetadata dataGeneratorMetadata)
    {
#pragma warning disable CS8603 // Possible null reference return.
        if (sharedType == SharedType.None)
        {
            return Create<T>(dataGeneratorMetadata);
        }

        if (sharedType == SharedType.PerTestSession)
        {
            return (T)(await TestDataContainer.GetGlobalInstanceAsync(typeof(T), () => Task.FromResult(Create(typeof(T), dataGeneratorMetadata))).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.PerClass)
        {
            return (T)(await TestDataContainer.GetInstanceForClassAsync(testClassType, typeof(T), () => Task.FromResult(Create(typeof(T), dataGeneratorMetadata))).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.Keyed)
        {
            return (T)(await TestDataContainer.GetInstanceForKeyAsync(key, typeof(T), () => Task.FromResult(Create(typeof(T), dataGeneratorMetadata))).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.PerAssembly)
        {
            return (T)(await TestDataContainer.GetInstanceForAssemblyAsync(testClassType.Assembly, typeof(T), () => Task.FromResult(Create(typeof(T), dataGeneratorMetadata))).ConfigureAwait(false))!;
        }
#pragma warning restore CS8603 // Possible null reference return.

        throw new ArgumentOutOfRangeException();
    }

    public async Task<object> GetAsync(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, Type testClassType, string? key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (sharedType == SharedType.None)
        {
            return Create(type, dataGeneratorMetadata);
        }

        if (sharedType == SharedType.PerTestSession)
        {
            return await TestDataContainer.GetGlobalInstanceAsync(type, () => Task.FromResult(Create(type, dataGeneratorMetadata))).ConfigureAwait(false);
        }

        if (sharedType == SharedType.PerClass)
        {
            return await TestDataContainer.GetInstanceForClassAsync(testClassType, type, () => Task.FromResult(Create(type, dataGeneratorMetadata))).ConfigureAwait(false);
        }

        if (sharedType == SharedType.Keyed)
        {
            return await TestDataContainer.GetInstanceForKeyAsync(key!, type, () => Task.FromResult(Create(type, dataGeneratorMetadata))).ConfigureAwait(false);
        }

        if (sharedType == SharedType.PerAssembly)
        {
            return await TestDataContainer.GetInstanceForAssemblyAsync(testClassType.Assembly, type, () => Task.FromResult(Create(type, dataGeneratorMetadata))).ConfigureAwait(false);
        }

        throw new ArgumentOutOfRangeException();
    }

    [return: NotNull]
    private static T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return ((T)Create(typeof(T), dataGeneratorMetadata))!;
    }

    private static object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, DataGeneratorMetadata dataGeneratorMetadata)
    {
        try
        {
            var instance = Activator.CreateInstance(type)!;
            
            // The framework will handle initialization and registration when the data is consumed
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


    public (T, SharedType, string) GetItemForIndex<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(int index, Type testClassType, SharedType[] sharedTypes, string[] keys, DataGeneratorMetadata dataGeneratorMetadata) where T : new()
    {
        return AsyncToSyncHelper.RunSync(() => GetItemForIndexAsync<T>(index, testClassType, sharedTypes, keys, dataGeneratorMetadata));
    }

    public T Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(SharedType sharedType, Type testClassType, string key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        return AsyncToSyncHelper.RunSync(() => GetAsync<T>(sharedType, testClassType, key, dataGeneratorMetadata));
    }

    public object Get(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, Type testClassType, string? key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        return AsyncToSyncHelper.RunSync(() => GetAsync(sharedType, type, testClassType, key, dataGeneratorMetadata));
    }
}
