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
            return await CreateAsync<T>(dataGeneratorMetadata).ConfigureAwait(false);
        }

        if (sharedType == SharedType.PerTestSession)
        {
            return (T)(await TestDataContainer.GetGlobalInstanceAsync(typeof(T), async () => await CreateAsync<T>(dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.PerClass)
        {
            return (T)(await TestDataContainer.GetInstanceForClassAsync(testClassType, typeof(T), async () => await CreateAsync<T>(dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.Keyed)
        {
            return (T)(await TestDataContainer.GetInstanceForKeyAsync(key, typeof(T), async () => await CreateAsync<T>(dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false))!;
        }

        if (sharedType == SharedType.PerAssembly)
        {
            return (T)(await TestDataContainer.GetInstanceForAssemblyAsync(testClassType.Assembly, typeof(T), async () => await CreateAsync<T>(dataGeneratorMetadata).ConfigureAwait(false)).ConfigureAwait(false))!;
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

            if (!Sources.Properties.TryGetValue(instance.GetType(), out var properties))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            }

            await InitializeDataSourcePropertiesAsync(dataGeneratorMetadata, instance, properties).ConfigureAwait(false);

            // Initialize the instance after all its properties have been set and initialized
            await ObjectInitializer.InitializeAsync(instance).ConfigureAwait(false);

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

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    private static async Task InitializeDataSourcePropertiesAsync(DataGeneratorMetadata dataGeneratorMetadata, object instance, PropertyInfo[] properties)
    {
        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.GetCustomAttributes().OfType<IDataSourceGeneratorAttribute>().FirstOrDefault() is not { } dataSourceGeneratorAttribute)
            {
                continue;
            }

            if (propertyInfo.GetValue(instance) is not {} result)
            {
                var resultDelegateArray = dataSourceGeneratorAttribute.Generate(dataGeneratorMetadata with
                {
                    Type = DataGeneratorType.Property, MembersToGenerate = [ReflectionToSourceModelHelpers.GenerateProperty(propertyInfo)]
                });

                result = resultDelegateArray.FirstOrDefault()?.Invoke()?.FirstOrDefault();

                propertyInfo.SetValue(instance, result);
            }

            if (result is null)
            {
                continue;
            }

            if (!Sources.Properties.TryGetValue(result.GetType(), out var nestedProperties))
            {
                nestedProperties = result.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            }

            await InitializeDataSourcePropertiesAsync(dataGeneratorMetadata, result, nestedProperties).ConfigureAwait(false);

            // Initialize the nested object after its properties have been set and initialized
            await ObjectInitializer.InitializeAsync(result).ConfigureAwait(false);
        }
    }

    // Synchronous wrapper methods for backward compatibility
    public (T, SharedType, string) GetItemForIndex<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(int index, Type testClassType, SharedType[] sharedTypes, string[] keys, DataGeneratorMetadata dataGeneratorMetadata) where T : new()
    {
        return Task.Run(async () => await GetItemForIndexAsync<T>(index, testClassType, sharedTypes, keys, dataGeneratorMetadata).ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    public T Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>(SharedType sharedType, Type testClassType, string key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        return Task.Run(async () => await GetAsync<T>(sharedType, testClassType, key, dataGeneratorMetadata).ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    public object Get(SharedType sharedType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, Type testClassType, string? key, DataGeneratorMetadata dataGeneratorMetadata)
    {
        return Task.Run(async () => await GetAsync(sharedType, type, testClassType, key, dataGeneratorMetadata).ConfigureAwait(false)).GetAwaiter().GetResult();
    }
}
