using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Data;
using TUnit.Core.Enums;
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
            return await CreateAsync<T>(dataGeneratorMetadata).ConfigureAwait(false);
        }

        if (sharedType == SharedType.PerTestSession)
        {
            return (T)(await TestDataContainer.GetGlobalInstanceAsync(typeof(T), async () => await CreateAsync(typeof(T), dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.PerClass)
        {
            return (T)(await TestDataContainer.GetInstanceForClassAsync(testClassType, typeof(T), async () => await CreateAsync(typeof(T), dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.Keyed)
        {
            return (T)(await TestDataContainer.GetInstanceForKeyAsync(key, typeof(T), async () => await CreateAsync(typeof(T), dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.PerAssembly)
        {
            return (T)(await TestDataContainer.GetInstanceForAssemblyAsync(testClassType.Assembly, typeof(T), async () => await CreateAsync(typeof(T), dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false))!;
        }
#pragma warning restore CS8603 // Possible null reference return.

        throw new ArgumentOutOfRangeException();
    }

    public async Task<object> GetAsync(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, Type testClassType, string? key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (sharedType == SharedType.None)
        {
            return await CreateAsync(type, dataGeneratorMetadata).ConfigureAwait(false);
        }

        if (sharedType == SharedType.PerTestSession)
        {
            return await TestDataContainer.GetGlobalInstanceAsync(type, async () => await CreateAsync(type, dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false);
        }

        if (sharedType == SharedType.PerClass)
        {
            return await TestDataContainer.GetInstanceForClassAsync(testClassType, type, async () => await CreateAsync(type, dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false);
        }

        if (sharedType == SharedType.Keyed)
        {
            return await TestDataContainer.GetInstanceForKeyAsync(key!, type, async () => await CreateAsync(type, dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false);
        }

        if (sharedType == SharedType.PerAssembly)
        {
            return await TestDataContainer.GetInstanceForAssemblyAsync(testClassType.Assembly, type, async () => await CreateAsync(type, dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false);
        }

        throw new ArgumentOutOfRangeException();
    }

    [return: NotNull]
    private static async Task<T> CreateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return ((T)(await CreateAsync(typeof(T), dataGeneratorMetadata).ConfigureAwait(false)))!;
    }

    private static async Task<object> CreateAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, DataGeneratorMetadata dataGeneratorMetadata)
    {
        try
        {
            var instance = Activator.CreateInstance(type)!;
            
            // Initialize data source properties on the created instance
            await Helpers.DataSourceInitializer.InitializeAsync(instance, dataGeneratorMetadata).ConfigureAwait(false);
            
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


}
