using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.PropertyInjection;
using TUnit.Core.PropertyInjection.Initialization;

namespace TUnit.Engine.Services;

/// <summary>
/// Orchestrates the entire property initialization process.
/// Coordinates between different components and manages the initialization flow.
/// </summary>
internal sealed class PropertyInitializationOrchestrator
{
    internal readonly DataSourceInitializer _dataSourceInitializer;
    private readonly IObjectRegistry _objectRegistry;

    public PropertyInitializationOrchestrator(DataSourceInitializer dataSourceInitializer, IObjectRegistry? objectRegistry)
    {
        _dataSourceInitializer = dataSourceInitializer ?? throw new ArgumentNullException(nameof(dataSourceInitializer));
        _objectRegistry = objectRegistry!;
    }

    /// <summary>
    /// Initializes all properties for an instance using source-generated metadata.
    /// Properties are initialized in parallel for better performance.
    /// </summary>
    private async Task InitializeSourceGeneratedPropertiesAsync(
        object instance,
        PropertyInjectionMetadata[] properties,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        if (properties.Length == 0)
        {
            return;
        }

        var tasks = properties.Select(metadata =>
            InitializeSourceGeneratedPropertyAsync(instance, metadata, objectBag, methodMetadata, events, visitedObjects));

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Initializes all properties for an instance using reflection.
    /// Properties are initialized in parallel for better performance.
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Reflection-based property initialization uses PropertyInfo")]
#endif
    private async Task InitializeReflectionPropertiesAsync(
        object instance,
        (PropertyInfo Property, IDataSourceAttribute DataSource)[] properties,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        if (properties.Length == 0)
        {
            return;
        }

        var tasks = properties.Select(pair =>
            InitializeReflectionPropertyAsync(instance, pair.Property, pair.DataSource, objectBag, methodMetadata, events, visitedObjects));

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Initializes a single property using source-generated metadata.
    /// </summary>
    #if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Property data resolver is called for source-generated properties which are AOT-safe")]
    #endif
    private async Task InitializeSourceGeneratedPropertyAsync(
        object instance,
        PropertyInjectionMetadata metadata,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        object? resolvedValue = null;
        var testContext = TestContext.Current;

        // Check if property was pre-resolved during registration
        if (testContext?.Metadata.TestDetails.TestClassInjectedPropertyArguments.TryGetValue(metadata.PropertyName, out resolvedValue) == true)
        {
            // Use pre-resolved value - it was already initialized during first resolution
        }
        else
        {
            // Resolve the property value from the data source
            resolvedValue = await PropertyDataResolver.ResolvePropertyDataAsync(
                new PropertyInitializationContext
                {
                    Instance = instance,
                    SourceGeneratedMetadata = metadata,
                    PropertyName = metadata.PropertyName,
                    PropertyType = metadata.PropertyType,
                    PropertySetter = metadata.SetProperty,
                    ObjectBag = objectBag,
                    MethodMetadata = methodMetadata,
                    Events = events,
                    VisitedObjects = visitedObjects,
                    TestContext = testContext,
                    IsNestedProperty = false
                },
                _dataSourceInitializer,
                _objectRegistry);

            if (resolvedValue == null)
            {
                return;
            }
        }

        // Set the property value
        metadata.SetProperty(instance, resolvedValue);

        // Store for potential reuse
        if (testContext != null && !testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments.ContainsKey(metadata.PropertyName))
        {
            testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments[metadata.PropertyName] = resolvedValue;
        }
    }

    /// <summary>
    /// Initializes a single property using reflection.
    /// </summary>
    #if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection-based property initialization is only used in reflection mode, not in AOT")]
    #endif
    private async Task InitializeReflectionPropertyAsync(
        object instance,
        PropertyInfo property,
        IDataSourceAttribute dataSource,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        var testContext = TestContext.Current;
        var propertySetter = PropertySetterFactory.CreateSetter(property);

        // Resolve the property value from the data source
        var resolvedValue = await PropertyDataResolver.ResolvePropertyDataAsync(
            new PropertyInitializationContext
            {
                Instance = instance,
                PropertyInfo = property,
                DataSource = dataSource,
                PropertyName = property.Name,
                PropertyType = property.PropertyType,
                PropertySetter = propertySetter,
                ObjectBag = objectBag,
                MethodMetadata = methodMetadata,
                Events = events,
                VisitedObjects = visitedObjects,
                TestContext = testContext,
                IsNestedProperty = false
            },
            _dataSourceInitializer,
            _objectRegistry);

        if (resolvedValue == null)
        {
            return;
        }

        // Set the property value
        propertySetter(instance, resolvedValue);
    }

    /// <summary>
    /// Handles the complete initialization flow for an object with properties.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Specific source gen path")]
    public async Task InitializeObjectWithPropertiesAsync(
        object instance,
        PropertyInjectionPlan plan,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        if (plan.HasProperties == false)
        {
            return;
        }

        // Initialize properties based on the mode (source-generated or reflection)
        if (SourceRegistrar.IsEnabled)
        {
            await InitializeSourceGeneratedPropertiesAsync(
                instance, plan.SourceGeneratedProperties, objectBag, methodMetadata, events, visitedObjects);
        }
        else
        {
            await InitializeReflectionPropertiesAsync(
                instance, plan.ReflectionProperties, objectBag, methodMetadata, events, visitedObjects);
        }
    }

}
