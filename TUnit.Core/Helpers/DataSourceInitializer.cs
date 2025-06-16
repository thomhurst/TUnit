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
        // Register the instance itself
        objectRegistrationCallback?.Invoke(instance);

        // Initialize nested data generators first
        await InitializeNestedDataGeneratorsAsync(instance, dataGeneratorMetadata, testBuilderContextAccessor, objectRegistrationCallback).ConfigureAwait(false);

        // Only initialize the object itself if it's a data attribute
        // This prevents initializing objects like WebApplicationFactory during data source generation
        if (instance is IDataAttribute)
        {
            await ObjectInitializer.InitializeAsync(instance).ConfigureAwait(false);
        }
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

        var objType = obj.GetType();
        
        // Skip primitive types, strings, and other types that shouldn't have nested properties processed
        if (objType.IsPrimitive || obj is string || objType.IsEnum || objType.IsValueType && !objType.IsGenericType)
        {
            return;
        }

        // Skip system types that don't have meaningful properties to process
        if (objType.Namespace?.StartsWith("System") == true)
        {
            return;
        }

        PropertyInfo[] properties;
        try
        {
            properties = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        }
        catch (NotSupportedException)
        {
            // Some types don't support reflection
            return;
        }

        foreach (var propertyInfo in properties)
        {
            var dataAttribute = propertyInfo.GetCustomAttributesSafe().OfType<IDataAttribute>().FirstOrDefault();
            var existingValue = propertyInfo.GetValue(obj);

            // Case 1: Property has a data attribute
            if (dataAttribute is not null)
            {
                if (existingValue is null)
                {
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
                        existingValue = value;
                    }
                }
            }

            // Case 2: Process any existing value (whether created above or already present)
            // This handles both values created by data attributes and values that need initialization
            if (existingValue is not null)
            {
                // Register the object
                objectRegistrationCallback?.Invoke(existingValue);

                // Recursively initialize if it's a data attribute or has nested properties
                await InitializeNestedDataGeneratorsInternalAsync(existingValue, dataGeneratorMetadata, testBuilderContextAccessor, objectRegistrationCallback, visited).ConfigureAwait(false);

                // Only initialize the value itself if it's a data attribute
                // Other objects should be initialized at test execution time
                if (existingValue is IDataAttribute)
                {
                    await ObjectInitializer.InitializeAsync(existingValue).ConfigureAwait(false);
                }
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
