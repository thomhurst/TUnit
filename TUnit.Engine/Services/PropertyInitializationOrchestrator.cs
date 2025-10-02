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
    private PropertyInitializationPipeline _pipeline;
    internal DataSourceInitializer DataSourceInitializer { get; }
    internal ObjectRegistrationService ObjectRegistrationService { get; private set; }

    public PropertyInitializationOrchestrator(DataSourceInitializer dataSourceInitializer, ObjectRegistrationService? objectRegistrationService)
    {
        DataSourceInitializer = dataSourceInitializer ?? throw new ArgumentNullException(nameof(dataSourceInitializer));
        ObjectRegistrationService = objectRegistrationService!; // Will be set via Initialize()
        if (objectRegistrationService != null)
        {
            _pipeline = PropertyInitializationPipeline.CreateDefault(dataSourceInitializer, objectRegistrationService);
        }
        else
        {
            _pipeline = null!; // Will be set via Initialize()
        }
    }

    public void Initialize(ObjectRegistrationService objectRegistrationService)
    {
        ObjectRegistrationService = objectRegistrationService ?? throw new ArgumentNullException(nameof(objectRegistrationService));
        _pipeline = PropertyInitializationPipeline.CreateDefault(DataSourceInitializer, objectRegistrationService);
    }

    /// <summary>
    /// Initializes all properties for an instance using source-generated metadata.
    /// </summary>
    public async Task InitializePropertiesAsync(
        object instance,
        PropertyInjectionMetadata[] properties,
        Dictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        if (properties.Length == 0)
        {
            return;
        }

        var contexts = properties.Select(metadata => CreateContext(
            instance, metadata, objectBag, methodMetadata, events, visitedObjects, TestContext.Current));

        await _pipeline.ExecuteParallelAsync(contexts);
    }

    /// <summary>
    /// Initializes all properties for an instance using reflection.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection mode support")]
    public async Task InitializePropertiesAsync(
        object instance,
        (PropertyInfo Property, IDataSourceAttribute DataSource)[] properties,
        Dictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        if (properties.Length == 0)
        {
            return;
        }

        var contexts = properties.Select(pair => CreateContext(
            instance, pair.Property, pair.DataSource, objectBag, methodMetadata, events, visitedObjects, TestContext.Current));

        await _pipeline.ExecuteParallelAsync(contexts);
    }

    /// <summary>
    /// Handles the complete initialization flow for an object with properties.
    /// </summary>
    public async Task InitializeObjectWithPropertiesAsync(
        object instance,
        PropertyInjectionPlan plan,
        Dictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        // Initialize properties based on the mode
        // Properties will be fully initialized (including nested initialization) by the strategies
        if (SourceRegistrar.IsEnabled)
        {
            await InitializePropertiesAsync(
                instance, plan.SourceGeneratedProperties, objectBag, methodMetadata, events, visitedObjects);
        }
        else
        {
            await InitializePropertiesAsync(
                instance, plan.ReflectionProperties, objectBag, methodMetadata, events, visitedObjects);
        }
    }

    /// <summary>
    /// Creates initialization context for source-generated properties.
    /// </summary>
    private PropertyInitializationContext CreateContext(
        object instance,
        PropertyInjectionMetadata metadata,
        Dictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        TestContext? testContext)
    {
        return new PropertyInitializationContext
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
        };
    }

    /// <summary>
    /// Creates initialization context for reflection-based properties.
    /// </summary>
    private PropertyInitializationContext CreateContext(
        object instance,
        PropertyInfo property,
        IDataSourceAttribute dataSource,
        Dictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        TestContext? testContext)
    {
        return new PropertyInitializationContext
        {
            Instance = instance,
            PropertyInfo = property,
            DataSource = dataSource,
            PropertyName = property.Name,
            PropertyType = property.PropertyType,
            PropertySetter = PropertySetterFactory.CreateSetter(property),
            ObjectBag = objectBag,
            MethodMetadata = methodMetadata,
            Events = events,
            VisitedObjects = visitedObjects,
            TestContext = testContext,
            IsNestedProperty = false
        };
    }

    /// <summary>
    /// Gets the singleton instance of the orchestrator.
    /// </summary>
}
