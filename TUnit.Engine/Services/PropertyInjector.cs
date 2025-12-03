using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.PropertyInjection;
using TUnit.Core.PropertyInjection.Initialization;

namespace TUnit.Engine.Services;

/// <summary>
/// Pure property injection service.
/// Follows Single Responsibility Principle - only injects property values, doesn't initialize objects.
/// Uses Lazy initialization to break circular dependencies without manual Initialize() calls.
/// </summary>
internal sealed class PropertyInjector
{
    private readonly Lazy<ObjectLifecycleService> _objectLifecycleService;
    private readonly string _testSessionId;

    // Object pool for visited dictionaries to reduce allocations
    private static readonly ConcurrentBag<ConcurrentDictionary<object, byte>> _visitedObjectsPool = new();

    public PropertyInjector(Lazy<ObjectLifecycleService> objectLifecycleService, string testSessionId)
    {
        _objectLifecycleService = objectLifecycleService;
        _testSessionId = testSessionId;
    }

    /// <summary>
    /// Resolves and caches property values for a test class type WITHOUT setting them on an instance.
    /// Used during registration to create shared objects early and enable proper reference counting.
    /// </summary>
    public async Task ResolveAndCachePropertiesAsync(
        Type testClassType,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        TestContext testContext)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(testClassType);

        if (!plan.HasProperties)
        {
            return;
        }

        // Resolve properties based on what's available in the plan
        if (plan.SourceGeneratedProperties.Length > 0)
        {
            await ResolveAndCacheSourceGeneratedPropertiesAsync(
                plan.SourceGeneratedProperties, objectBag, methodMetadata, events, testContext);
        }
        else if (plan.ReflectionProperties.Length > 0)
        {
            await ResolveAndCacheReflectionPropertiesAsync(
                plan.ReflectionProperties, objectBag, methodMetadata, events, testContext);
        }
    }

    /// <summary>
    /// Injects properties into an object and recursively into nested objects.
    /// </summary>
    public async Task InjectPropertiesAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (objectBag == null)
        {
            throw new ArgumentNullException(nameof(objectBag));
        }

        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        // Rent dictionary from pool
        if (!_visitedObjectsPool.TryTake(out var visitedObjects))
        {
#if NETSTANDARD2_0
            visitedObjects = new ConcurrentDictionary<object, byte>();
#else
            visitedObjects = new ConcurrentDictionary<object, byte>(ReferenceEqualityComparer.Instance);
#endif
        }

        try
        {
            await InjectPropertiesRecursiveAsync(instance, objectBag, methodMetadata, events, visitedObjects);
        }
        finally
        {
            visitedObjects.Clear();
            _visitedObjectsPool.Add(visitedObjects);
        }
    }

    /// <summary>
    /// Injects properties into multiple argument objects in parallel.
    /// </summary>
    public async Task InjectPropertiesIntoArgumentsAsync(
        object?[] arguments,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata methodMetadata,
        TestContextEvents events)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        var injectableArgs = arguments
            .Where(arg => arg != null && PropertyInjectionCache.HasInjectableProperties(arg.GetType()))
            .ToArray();

        if (injectableArgs.Length == 0)
        {
            return;
        }

        await Task.WhenAll(injectableArgs.Select(arg =>
            InjectPropertiesAsync(arg!, objectBag, methodMetadata, events)));
    }

    private async Task InjectPropertiesRecursiveAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        if (instance == null)
        {
            return;
        }

        // Prevent cycles
        if (!visitedObjects.TryAdd(instance, 0))
        {
            return;
        }

        try
        {
            var plan = PropertyInjectionCache.GetOrCreatePlan(instance.GetType());

            if (plan.HasProperties)
            {
                // Initialize properties based on what's available in the plan
                if (plan.SourceGeneratedProperties.Length > 0)
                {
                    await InjectSourceGeneratedPropertiesAsync(
                        instance, plan.SourceGeneratedProperties, objectBag, methodMetadata, events, visitedObjects);
                }
                else if (plan.ReflectionProperties.Length > 0)
                {
                    await InjectReflectionPropertiesAsync(
                        instance, plan.ReflectionProperties, objectBag, methodMetadata, events, visitedObjects);
                }
            }

            // Recurse into nested properties
            await RecurseIntoNestedPropertiesAsync(instance, plan, objectBag, methodMetadata, events, visitedObjects);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to inject properties for type '{instance.GetType().Name}': {ex.Message}", ex);
        }
    }

    private async Task InjectSourceGeneratedPropertiesAsync(
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

        // Initialize properties in parallel
        await Task.WhenAll(properties.Select(metadata =>
            InjectSourceGeneratedPropertyAsync(instance, metadata, objectBag, methodMetadata, events, visitedObjects)));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Source-gen properties are AOT-safe")]
    private async Task InjectSourceGeneratedPropertyAsync(
        object instance,
        PropertyInjectionMetadata metadata,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        // First check if the property already has a value - skip if it does
        // This handles nested objects that were already constructed with their properties set
        var property = metadata.ContainingType.GetProperty(metadata.PropertyName);
        if (property != null && property.CanRead)
        {
            var existingValue = property.GetValue(instance);
            if (existingValue != null)
            {
                // Property already has a value, don't overwrite it
                return;
            }
        }

        var testContext = TestContext.Current;
        object? resolvedValue = null;

        // Use a composite key to avoid conflicts when nested classes have properties with the same name
        var cacheKey = $"{metadata.ContainingType.FullName}.{metadata.PropertyName}";

        // Check if property was pre-resolved during registration
        if (testContext?.Metadata.TestDetails.TestClassInjectedPropertyArguments.TryGetValue(cacheKey, out resolvedValue) == true)
        {
            // Use pre-resolved value
        }
        else
        {
            // Resolve the property value from the data source
            resolvedValue = await ResolvePropertyDataAsync(
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
                });

            if (resolvedValue == null)
            {
                return;
            }
        }

        // Set the property value
        metadata.SetProperty(instance, resolvedValue);

        // Store for potential reuse with composite key
        if (testContext != null)
        {
            ((ConcurrentDictionary<string, object?>)testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments)
                .TryAdd(cacheKey, resolvedValue);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT")]
    private async Task InjectReflectionPropertiesAsync(
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

        await Task.WhenAll(properties.Select(pair =>
            InjectReflectionPropertyAsync(instance, pair.Property, pair.DataSource, objectBag, methodMetadata, events, visitedObjects)));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT")]
    private async Task InjectReflectionPropertyAsync(
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

        var resolvedValue = await ResolvePropertyDataAsync(
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
            });

        if (resolvedValue == null)
        {
            return;
        }

        propertySetter(instance, resolvedValue);
    }

    private async Task RecurseIntoNestedPropertiesAsync(
        object instance,
        PropertyInjectionPlan plan,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        if (!plan.HasProperties)
        {
            return;
        }

        if (plan.SourceGeneratedProperties.Length > 0)
        {
            foreach (var metadata in plan.SourceGeneratedProperties)
            {
                var property = metadata.ContainingType.GetProperty(metadata.PropertyName);
                if (property == null || !property.CanRead)
                {
                    continue;
                }

                var propertyValue = property.GetValue(instance);
                if (propertyValue == null)
                {
                    continue;
                }

                if (PropertyInjectionCache.HasInjectableProperties(propertyValue.GetType()))
                {
                    await InjectPropertiesRecursiveAsync(propertyValue, objectBag, methodMetadata, events, visitedObjects);
                }
            }
        }
        else if (plan.ReflectionProperties.Length > 0)
        {
            foreach (var (property, _) in plan.ReflectionProperties)
            {
                var propertyValue = property.GetValue(instance);
                if (propertyValue == null)
                {
                    continue;
                }

                if (PropertyInjectionCache.HasInjectableProperties(propertyValue.GetType()))
                {
                    await InjectPropertiesRecursiveAsync(propertyValue, objectBag, methodMetadata, events, visitedObjects);
                }
            }
        }
    }

    private async Task ResolveAndCacheSourceGeneratedPropertiesAsync(
        PropertyInjectionMetadata[] properties,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        TestContext testContext)
    {
        if (properties.Length == 0)
        {
            return;
        }

        // Resolve properties in parallel
        await Task.WhenAll(properties.Select(metadata =>
            ResolveAndCacheSourceGeneratedPropertyAsync(metadata, objectBag, methodMetadata, events, testContext)));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Source-gen properties are AOT-safe")]
    private async Task ResolveAndCacheSourceGeneratedPropertyAsync(
        PropertyInjectionMetadata metadata,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        TestContext testContext)
    {
        var cacheKey = $"{metadata.ContainingType.FullName}.{metadata.PropertyName}";

        // Check if already cached
        if (testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments.ContainsKey(cacheKey))
        {
            return;
        }

        // Resolve the property value from the data source
        var resolvedValue = await ResolvePropertyDataAsync(
            new PropertyInitializationContext
            {
                Instance = PlaceholderInstance.Instance,  // Use placeholder during registration
                SourceGeneratedMetadata = metadata,
                PropertyName = metadata.PropertyName,
                PropertyType = metadata.PropertyType,
                PropertySetter = metadata.SetProperty,
                ObjectBag = objectBag,
                MethodMetadata = methodMetadata,
                Events = events,
                VisitedObjects = new ConcurrentDictionary<object, byte>(),  // Empty dictionary for cycle detection
                TestContext = testContext,
                IsNestedProperty = false
            });

        if (resolvedValue != null)
        {
            // Cache the resolved value
            ((ConcurrentDictionary<string, object?>)testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments)
                .TryAdd(cacheKey, resolvedValue);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT")]
    private async Task ResolveAndCacheReflectionPropertiesAsync(
        (PropertyInfo Property, IDataSourceAttribute DataSource)[] properties,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        TestContext testContext)
    {
        if (properties.Length == 0)
        {
            return;
        }

        await Task.WhenAll(properties.Select(pair =>
            ResolveAndCacheReflectionPropertyAsync(pair.Property, pair.DataSource, objectBag, methodMetadata, events, testContext)));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT")]
    private async Task ResolveAndCacheReflectionPropertyAsync(
        PropertyInfo property,
        IDataSourceAttribute dataSource,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        TestContext testContext)
    {
        var cacheKey = $"{property.DeclaringType!.FullName}.{property.Name}";

        // Check if already cached
        if (testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments.ContainsKey(cacheKey))
        {
            return;
        }

        var propertySetter = PropertySetterFactory.CreateSetter(property);

        var resolvedValue = await ResolvePropertyDataAsync(
            new PropertyInitializationContext
            {
                Instance = PlaceholderInstance.Instance,  // Use placeholder during registration
                PropertyInfo = property,
                DataSource = dataSource,
                PropertyName = property.Name,
                PropertyType = property.PropertyType,
                PropertySetter = propertySetter,
                ObjectBag = objectBag,
                MethodMetadata = methodMetadata,
                Events = events,
                VisitedObjects = new ConcurrentDictionary<object, byte>(),  // Empty dictionary for cycle detection
                TestContext = testContext,
                IsNestedProperty = false
            });

        if (resolvedValue != null)
        {
            // Cache the resolved value
            ((ConcurrentDictionary<string, object?>)testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments)
                .TryAdd(cacheKey, resolvedValue);
        }
    }

    /// <summary>
    /// Resolves data from a property's data source.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Property data resolution handles both modes")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "PropertyType is properly preserved through source generation")]
    private async Task<object?> ResolvePropertyDataAsync(PropertyInitializationContext context)
    {
        var dataSource = await GetInitializedDataSourceAsync(context);
        if (dataSource == null)
        {
            return null;
        }

        var dataGeneratorMetadata = CreateDataGeneratorMetadata(context, dataSource);
        var dataRows = dataSource.GetDataRowsAsync(dataGeneratorMetadata);

        await foreach (var factory in dataRows)
        {
            var args = await factory();
            var value = TupleValueResolver.ResolveTupleValue(context.PropertyType, args);

            // Resolve any Func<T> wrappers
            value = await PropertyValueProcessor.ResolveTestDataValueAsync(typeof(object), value);

            if (value != null)
            {
                // Ensure nested objects are initialized
                if (PropertyInjectionCache.HasInjectableProperties(value.GetType()) || value is IAsyncInitializer)
                {
                    await _objectLifecycleService.Value.EnsureInitializedAsync(
                        value,
                        context.ObjectBag,
                        context.MethodMetadata,
                        context.Events);
                }

                return value;
            }
        }

        return null;
    }

    private async Task<IDataSourceAttribute?> GetInitializedDataSourceAsync(PropertyInitializationContext context)
    {
        IDataSourceAttribute? dataSource = null;

        if (context.DataSource != null)
        {
            dataSource = context.DataSource;
        }
        else if (context.SourceGeneratedMetadata != null)
        {
            dataSource = context.SourceGeneratedMetadata.CreateDataSource();
        }

        if (dataSource == null)
        {
            return null;
        }

        // Ensure the data source is initialized
        return await _objectLifecycleService.Value.EnsureInitializedAsync(
            dataSource,
            context.ObjectBag,
            context.MethodMetadata,
            context.Events);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Metadata creation handles both modes")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "ContainingType and PropertyType are preserved through source generation")]
    private DataGeneratorMetadata CreateDataGeneratorMetadata(
        PropertyInitializationContext context,
        IDataSourceAttribute dataSource)
    {
        if (context.SourceGeneratedMetadata != null)
        {
            if (context.SourceGeneratedMetadata.ContainingType == null)
            {
                throw new InvalidOperationException(
                    $"ContainingType is null for property '{context.PropertyName}'.");
            }

            var propertyMetadata = new PropertyMetadata
            {
                IsStatic = false,
                Name = context.PropertyName,
                ClassMetadata = ClassMetadataHelper.GetOrCreateClassMetadata(context.SourceGeneratedMetadata.ContainingType),
                Type = context.PropertyType,
                ReflectionInfo = PropertyHelper.GetPropertyInfo(context.SourceGeneratedMetadata.ContainingType, context.PropertyName),
                Getter = parent => PropertyHelper.GetPropertyInfo(context.SourceGeneratedMetadata.ContainingType, context.PropertyName).GetValue(parent!)!,
                ContainingTypeMetadata = ClassMetadataHelper.GetOrCreateClassMetadata(context.SourceGeneratedMetadata.ContainingType)
            };

            return DataGeneratorMetadataCreator.CreateForPropertyInjection(
                propertyMetadata,
                context.MethodMetadata,
                dataSource,
                _testSessionId,
                context.TestContext,
                context.TestContext?.Metadata.TestDetails.ClassInstance,
                context.Events,
                context.ObjectBag);
        }
        else if (context.PropertyInfo != null)
        {
            return DataGeneratorMetadataCreator.CreateForPropertyInjection(
                context.PropertyInfo,
                context.PropertyInfo.DeclaringType!,
                context.MethodMetadata,
                dataSource,
                _testSessionId,
                context.TestContext,
                context.Instance,
                context.Events,
                context.ObjectBag);
        }

        throw new InvalidOperationException("Cannot create data generator metadata: no property information available");
    }
}
