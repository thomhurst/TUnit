using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.PropertyInjection;
using TUnit.Core.PropertyInjection.Initialization;

namespace TUnit.Engine.Services.PropertyInitialization;

/// <summary>
/// Strategy for handling nested property initialization.
/// Manages recursive property injection for complex object graphs.
/// </summary>
internal sealed class NestedPropertyStrategy : IPropertyInitializationStrategy
{
    private readonly DataSourceInitializer _dataSourceInitializer;
    private readonly ObjectRegistrationService _objectRegistrationService;

    public NestedPropertyStrategy(DataSourceInitializer dataSourceInitializer, ObjectRegistrationService objectRegistrationService)
    {
        _dataSourceInitializer = dataSourceInitializer ?? throw new ArgumentNullException(nameof(dataSourceInitializer));
        _objectRegistrationService = objectRegistrationService ?? throw new ArgumentNullException(nameof(objectRegistrationService));
    }

    public NestedPropertyStrategy()
    {
        // Default constructor for backward compatibility if needed
        _dataSourceInitializer = null!;
        _objectRegistrationService = null!;
    }
    /// <summary>
    /// Determines if this strategy can handle nested properties.
    /// </summary>
    public bool CanHandle(PropertyInitializationContext context)
    {
        return context.IsNestedProperty && context.ResolvedValue != null;
    }

    /// <summary>
    /// Initializes nested properties within an already resolved property value.
    /// </summary>
    #if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Property injection cache and setter factory handle both AOT and reflection modes appropriately")]
    #endif
    public async Task InitializePropertyAsync(PropertyInitializationContext context)
    {
        if (context.ResolvedValue == null)
        {
            return;
        }

        var propertyValue = context.ResolvedValue;
        var propertyType = propertyValue.GetType();

        // Check if we've already processed this object (cycle detection)
        if (!context.VisitedObjects.TryAdd(propertyValue, 0))
        {
            return; // Already processing or processed
        }

        // Get the injection plan for this type
        var plan = PropertyInjectionCache.GetOrCreatePlan(propertyType);

        // Recursively inject properties into the nested object
        if (SourceRegistrar.IsEnabled)
        {
            await ProcessSourceGeneratedNestedProperties(context, propertyValue, plan);
        }
        else
        {
            await ProcessReflectionNestedProperties(context, propertyValue, plan);
        }
    }

    /// <summary>
    /// Processes nested properties using source-generated metadata.
    /// </summary>
    private async Task ProcessSourceGeneratedNestedProperties(
        PropertyInitializationContext parentContext,
        object instance,
        PropertyInjectionPlan plan)
    {
        var tasks = plan.SourceGeneratedProperties.Select(async metadata =>
        {
            var nestedContext = CreateNestedContext(parentContext, instance, metadata);
            var strategy = new SourceGeneratedPropertyStrategy(_dataSourceInitializer, _objectRegistrationService);

            if (strategy.CanHandle(nestedContext))
            {
                await strategy.InitializePropertyAsync(nestedContext);
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Processes nested properties using reflection.
    /// </summary>
    private async Task ProcessReflectionNestedProperties(
        PropertyInitializationContext parentContext,
        object instance,
        PropertyInjectionPlan plan)
    {
        var tasks = plan.ReflectionProperties.Select(async pair =>
        {
            var nestedContext = CreateNestedContext(parentContext, instance, pair.Property, pair.DataSource);
            var strategy = new ReflectionPropertyStrategy(_dataSourceInitializer, _objectRegistrationService);

            if (strategy.CanHandle(nestedContext))
            {
                await strategy.InitializePropertyAsync(nestedContext);
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Creates a nested context for source-generated properties.
    /// </summary>
    private PropertyInitializationContext CreateNestedContext(
        PropertyInitializationContext parentContext,
        object instance,
        TUnit.Core.Interfaces.SourceGenerator.PropertyInjectionMetadata metadata)
    {
        // Build hierarchical property name for nested properties
        var propertyName = parentContext.IsNestedProperty
            ? $"{parentContext.PropertyName}.{metadata.PropertyName}"
            : metadata.PropertyName;

        return new PropertyInitializationContext
        {
            Instance = instance,
            SourceGeneratedMetadata = metadata,
            PropertyName = propertyName,
            PropertyType = metadata.PropertyType,
            PropertySetter = metadata.SetProperty,
            ObjectBag = parentContext.ObjectBag,
            MethodMetadata = parentContext.MethodMetadata,
            Events = parentContext.Events,
            VisitedObjects = parentContext.VisitedObjects,
            TestContext = parentContext.TestContext,
            IsNestedProperty = true,
            ParentInstance = parentContext.Instance
        };
    }

    /// <summary>
    /// Creates a nested context for reflection-based properties.
    /// </summary>
    #if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection-based property setter creation is only used in reflection mode, not in AOT")]
    #endif
    private PropertyInitializationContext CreateNestedContext(
        PropertyInitializationContext parentContext,
        object instance,
        System.Reflection.PropertyInfo property,
        IDataSourceAttribute dataSource)
    {
        // Build hierarchical property name for nested properties
        var propertyName = parentContext.IsNestedProperty
            ? $"{parentContext.PropertyName}.{property.Name}"
            : property.Name;

        return new PropertyInitializationContext
        {
            Instance = instance,
            PropertyInfo = property,
            DataSource = dataSource,
            PropertyName = propertyName,
            PropertyType = property.PropertyType,
            PropertySetter = PropertySetterFactory.CreateSetter(property),
            ObjectBag = parentContext.ObjectBag,
            MethodMetadata = parentContext.MethodMetadata,
            Events = parentContext.Events,
            VisitedObjects = parentContext.VisitedObjects,
            TestContext = parentContext.TestContext,
            IsNestedProperty = true,
            ParentInstance = parentContext.Instance
        };
    }
}
