using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

/// <summary>
/// Provides centralized logic for initializing data sources and their properties.
/// This follows the DRY principle by consolidating initialization logic used across the framework.
/// </summary>
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
[UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
internal static class DataSourceInitializer
{
    /// <summary>
    /// Initializes an object and all its properties that have data attributes.
    /// </summary>
    /// <param name="instance">The instance to initialize</param>
    /// <param name="dataGeneratorMetadata">Metadata for data generation</param>
    /// <param name="testBuilderContextAccessor">Optional test builder context (used by engine)</param>
    /// <param name="objectRegistrationCallback">Optional callback to register created objects</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task InitializeAsync(
        object instance,
        DataGeneratorMetadata dataGeneratorMetadata,
        TestBuilderContextAccessor? testBuilderContextAccessor = null,
        Action<object>? objectRegistrationCallback = null)
    {
        if (instance is null)
        {
            return;
        }

        // Register the instance itself
        objectRegistrationCallback?.Invoke(instance);

        // Initialize nested data generators first
        await InitializeNestedDataGeneratorsAsync(instance, dataGeneratorMetadata, testBuilderContextAccessor, objectRegistrationCallback).ConfigureAwait(false);

        // Then initialize the object itself
        await ObjectInitializer.InitializeAsync(instance).ConfigureAwait(false);
    }

    /// <summary>
    /// Initializes all properties with data attributes on the given object.
    /// </summary>
    private static async Task InitializeNestedDataGeneratorsAsync(
        object instance,
        DataGeneratorMetadata dataGeneratorMetadata,
        TestBuilderContextAccessor? testBuilderContextAccessor,
        Action<object>? objectRegistrationCallback)
    {
        var visited = new HashSet<object>();
        await InitializeNestedDataGeneratorsInternalAsync(instance, dataGeneratorMetadata, testBuilderContextAccessor, objectRegistrationCallback, visited).ConfigureAwait(false);
    }

    private static async Task InitializeNestedDataGeneratorsInternalAsync(
        object? obj,
        DataGeneratorMetadata dataGeneratorMetadata,
        TestBuilderContextAccessor? testBuilderContextAccessor,
        Action<object>? objectRegistrationCallback,
        HashSet<object> visited)
    {
        if (obj is null || !visited.Add(obj))
        {
            return;
        }

        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        foreach (var propertyInfo in properties)
        {
            // Skip if property doesn't have a data attribute
            var dataAttribute = propertyInfo.GetCustomAttributesSafe().OfType<IDataAttribute>().FirstOrDefault();
            if (dataAttribute is null)
            {
                continue;
            }

            // Skip if property already has a value
            if (propertyInfo.GetValue(obj) is not null)
            {
                continue;
            }

            // First, recursively initialize the data attribute itself if needed
            if (dataAttribute is IAsyncDataSourceGeneratorAttribute)
            {
                await InitializeNestedDataGeneratorsInternalAsync(dataAttribute, dataGeneratorMetadata, testBuilderContextAccessor, objectRegistrationCallback, visited).ConfigureAwait(false);
            }

            // Create value for the property
            var value = await CreatePropertyValueAsync(obj, propertyInfo, dataAttribute, dataGeneratorMetadata, testBuilderContextAccessor).ConfigureAwait(false);

            if (value is not null)
            {
                propertyInfo.SetValue(obj, value);

                // Register the created object
                objectRegistrationCallback?.Invoke(value);

                // Recursively initialize the created value
                await InitializeNestedDataGeneratorsInternalAsync(value, dataGeneratorMetadata, testBuilderContextAccessor, objectRegistrationCallback, visited).ConfigureAwait(false);

                // Initialize the value itself
                await ObjectInitializer.InitializeAsync(value).ConfigureAwait(false);
            }
        }
    }

    private static async Task<object?> CreatePropertyValueAsync(
        object instance,
        PropertyInfo propertyInfo,
        IDataAttribute dataAttribute,
        DataGeneratorMetadata dataGeneratorMetadata,
        TestBuilderContextAccessor? testBuilderContextAccessor)
    {
        // For engine context (with TestBuilderContextAccessor), use ReflectionValueCreator
        if (testBuilderContextAccessor is not null)
        {
            var classInformation = ReflectionToSourceModelHelpers.GenerateClass(instance.GetType());
            var propertyInformation = ReflectionToSourceModelHelpers.GenerateProperty(propertyInfo);

            var propertyMetadata = dataGeneratorMetadata with
            {
                Type = DataGeneratorType.Property,
                MembersToGenerate = [propertyInformation]
            };

            return await ReflectionValueCreator.CreatePropertyValueAsync(
                classInformation,
                testBuilderContextAccessor,
                dataAttribute,
                propertyInformation,
                propertyMetadata).ConfigureAwait(false);
        }

        // For user context (without TestBuilderContextAccessor), handle async data sources
        if (dataAttribute is IAsyncDataSourceGeneratorAttribute asyncDataSource)
        {
            var propertyMetadata = dataGeneratorMetadata with
            {
                Type = DataGeneratorType.Property,
                MembersToGenerate = [ReflectionToSourceModelHelpers.GenerateProperty(propertyInfo)]
            };

            var asyncEnumerable = asyncDataSource.GenerateAsync(propertyMetadata);
            await using var enumerator = asyncEnumerable.GetAsyncEnumerator();

            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                var func = enumerator.Current;
                var resultArray = await func().ConfigureAwait(false);
                return resultArray?.FirstOrDefault();
            }
        }

        return null;
    }
}
