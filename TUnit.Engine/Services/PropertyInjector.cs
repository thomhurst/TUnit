using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Helpers;
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
/// <remarks>
/// Depends on <see cref="IInitializationCallback"/> rather than a concrete service,
/// enabling testability and following Dependency Inversion Principle.
/// </remarks>
internal sealed class PropertyInjector
{
    private readonly Lazy<IInitializationCallback> _initializationCallback;
    private readonly string _testSessionId;

    // Object pool for visited dictionaries to reduce allocations
    private static readonly ConcurrentBag<ConcurrentDictionary<object, byte>> _visitedObjectsPool = new();

    // Cache for PropertyInfo lookups by (ContainingType, PropertyName) to avoid repeated reflection
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propertyInfoCache = new();

    public PropertyInjector(Lazy<IInitializationCallback> initializationCallback, string testSessionId)
    {
        _initializationCallback = initializationCallback;
        _testSessionId = testSessionId;
    }

    /// <summary>
    /// Resolves and caches property values for a test class type WITHOUT setting them on an instance.
    /// Used during registration to create shared objects early and enable proper reference counting.
    /// </summary>
    public Task ResolveAndCachePropertiesAsync(
        Type testClassType,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        TestContext testContext,
        CancellationToken cancellationToken = default)
    {
        // Skip property resolution if this test is reusing the discovery instance (already initialized)
        if (testContext.IsDiscoveryInstanceReused)
        {
            return Task.CompletedTask;
        }

        var plan = PropertyInjectionCache.GetOrCreatePlan(testClassType);

        if (!plan.HasProperties)
        {
            return Task.CompletedTask;
        }

        return ResolveAndCachePropertiesCoreAsync(objectBag, methodMetadata, events, testContext, plan, cancellationToken);
    }

    private Task ResolveAndCachePropertiesCoreAsync(
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        TestContext testContext,
        PropertyInjectionPlan plan,
        CancellationToken cancellationToken)
    {
        if (plan.SourceGeneratedProperties.Length > 0)
        {
            return ResolveAndCacheSourceGeneratedPropertiesAsync(
                plan.SourceGeneratedProperties, objectBag, methodMetadata, events, testContext, cancellationToken);
        }

        if (plan.ReflectionProperties.Length > 0)
        {
            return ResolveAndCacheReflectionPropertiesAsync(
                plan.ReflectionProperties, objectBag, methodMetadata, events, testContext, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Injects properties into an object and recursively into nested objects.
    /// </summary>
    public async Task InjectPropertiesAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        CancellationToken cancellationToken = default)
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

        var visitedObjects = RentVisitedDictionary();

        try
        {
            await InjectPropertiesRecursiveAsync(instance, objectBag, methodMetadata, events, visitedObjects, cancellationToken);
        }
        finally
        {
            ReturnVisitedDictionary(visitedObjects);
        }
    }

    /// <summary>
    /// Injects properties into multiple argument objects in parallel.
    /// </summary>
    public async Task InjectPropertiesIntoArgumentsAsync(
        object?[] arguments,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata methodMetadata,
        TestContextEvents events,
        CancellationToken cancellationToken = default)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        // Build list of injectable args without LINQ
        var injectableArgs = new List<object>(arguments.Length);
        for (var i = 0; i < arguments.Length; i++)
        {
            var arg = arguments[i];
            if (arg != null && PropertyInjectionCache.HasInjectableProperties(arg.GetType()))
            {
                injectableArgs.Add(arg);
            }
        }

        if (injectableArgs.Count == 0)
        {
            return;
        }

        // Build task list without LINQ Select
        var tasks = new List<Task>(injectableArgs.Count);
        for (var i = 0; i < injectableArgs.Count; i++)
        {
            tasks.Add(InjectPropertiesAsync(injectableArgs[i], objectBag, methodMetadata, events, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task InjectPropertiesRecursiveAsync(
        object? instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        CancellationToken cancellationToken)
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
                await InjectPropertiesFromPlanAsync(instance, plan, objectBag, methodMetadata, events, visitedObjects, cancellationToken);
            }

            // Recurse into nested properties
            await RecurseIntoNestedPropertiesAsync(instance, plan, objectBag, methodMetadata, events, visitedObjects, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to inject properties for type '{instance.GetType().Name}': {ex.Message}", ex);
        }
    }

    private Task InjectPropertiesFromPlanAsync(
        object instance,
        PropertyInjectionPlan plan,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        CancellationToken cancellationToken)
    {
        if (plan.SourceGeneratedProperties.Length > 0)
        {
            return InjectSourceGeneratedPropertiesAsync(
                instance, plan.SourceGeneratedProperties, objectBag, methodMetadata, events, visitedObjects, cancellationToken);
        }

        if (plan.ReflectionProperties.Length > 0)
        {
            return InjectReflectionPropertiesAsync(
                instance, plan.ReflectionProperties, objectBag, methodMetadata, events, visitedObjects, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private Task InjectSourceGeneratedPropertiesAsync(
        object instance,
        PropertyInjectionMetadata[] properties,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        CancellationToken cancellationToken)
    {
        return ParallelTaskHelper.ForEachAsync(properties,
            prop => InjectSourceGeneratedPropertyAsync(instance, prop, objectBag, methodMetadata, events, visitedObjects, cancellationToken));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Source-gen properties are AOT-safe")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ContainingType is annotated with DynamicallyAccessedMembers in PropertyInjectionMetadata")]
    private async Task InjectSourceGeneratedPropertyAsync(
        object instance,
        PropertyInjectionMetadata metadata,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        CancellationToken cancellationToken)
    {
        // First check if the property already has a value - skip if it does
        // This handles nested objects that were already constructed with their properties set
        var property = GetCachedPropertyInfo(metadata.ContainingType, metadata.PropertyName);
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
        var cacheKey = PropertyCacheKeyGenerator.GetCacheKey(metadata);

        // Check if property was pre-resolved during registration
        if (testContext?.Metadata.TestDetails.TestClassInjectedPropertyArguments.TryGetValue(cacheKey, out resolvedValue) != true)
        {
            // Resolve the property value from the data source
            resolvedValue = await ResolvePropertyDataAsync(
                PropertyInitializationContext.ForSourceGenerated(
                    instance, metadata, objectBag, methodMetadata, events, visitedObjects, testContext),
                cancellationToken);

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
    private Task InjectReflectionPropertiesAsync(
        object instance,
        (PropertyInfo Property, IDataSourceAttribute DataSource)[] properties,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        CancellationToken cancellationToken)
    {
        return ParallelTaskHelper.ForEachAsync(properties,
            pair => InjectReflectionPropertyAsync(instance, pair.Property, pair.DataSource, objectBag, methodMetadata, events, visitedObjects, cancellationToken));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT")]
    private async Task InjectReflectionPropertyAsync(
        object instance,
        PropertyInfo property,
        IDataSourceAttribute dataSource,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        CancellationToken cancellationToken)
    {
        var testContext = TestContext.Current;
        var propertySetter = PropertySetterFactory.CreateSetter(property);

        var resolvedValue = await ResolvePropertyDataAsync(
            PropertyInitializationContext.ForReflection(
                instance, property, dataSource, propertySetter, objectBag, methodMetadata, events, visitedObjects, testContext),
            cancellationToken);

        if (resolvedValue == null)
        {
            return;
        }

        propertySetter(instance, resolvedValue);
    }

    private Task RecurseIntoNestedPropertiesAsync(
        object instance,
        PropertyInjectionPlan plan,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        CancellationToken cancellationToken)
    {
        if (!plan.HasProperties)
        {
            return Task.CompletedTask;
        }

        return RecurseIntoNestedPropertiesCoreAsync(instance, plan, objectBag, methodMetadata, events, visitedObjects, cancellationToken);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ContainingType is annotated with DynamicallyAccessedMembers in PropertyInjectionMetadata")]
    private async Task RecurseIntoNestedPropertiesCoreAsync(
        object instance,
        PropertyInjectionPlan plan,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        CancellationToken cancellationToken)
    {
        if (plan.SourceGeneratedProperties.Length > 0)
        {
            for (var i = 0; i < plan.SourceGeneratedProperties.Length; i++)
            {
                var metadata = plan.SourceGeneratedProperties[i];
                var property = GetCachedPropertyInfo(metadata.ContainingType, metadata.PropertyName);
                if (property == null || !property.CanRead)
                {
                    continue;
                }

                await RecurseIntoPropertyValueAsync(property.GetValue(instance), objectBag, methodMetadata, events, visitedObjects, cancellationToken);
            }
        }
        else if (plan.ReflectionProperties.Length > 0)
        {
            for (var i = 0; i < plan.ReflectionProperties.Length; i++)
            {
                var (property, _) = plan.ReflectionProperties[i];
                await RecurseIntoPropertyValueAsync(property.GetValue(instance), objectBag, methodMetadata, events, visitedObjects, cancellationToken);
            }
        }
    }

    private Task RecurseIntoPropertyValueAsync(
        object? propertyValue,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects,
        CancellationToken cancellationToken)
    {
        if (propertyValue == null)
        {
            return Task.CompletedTask;
        }

        if (!PropertyInjectionCache.HasInjectableProperties(propertyValue.GetType()))
        {
            return Task.CompletedTask;
        }

        return InjectPropertiesRecursiveAsync(propertyValue, objectBag, methodMetadata, events, visitedObjects, cancellationToken);
    }

    private Task ResolveAndCacheSourceGeneratedPropertiesAsync(
        PropertyInjectionMetadata[] properties,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        TestContext testContext,
        CancellationToken cancellationToken)
    {
        return ParallelTaskHelper.ForEachAsync(properties,
            prop => ResolveAndCacheSourceGeneratedPropertyAsync(prop, objectBag, methodMetadata, events, testContext, cancellationToken));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Source-gen properties are AOT-safe")]
    private async Task ResolveAndCacheSourceGeneratedPropertyAsync(
        PropertyInjectionMetadata metadata,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        TestContext testContext,
        CancellationToken cancellationToken)
    {
        var cacheKey = PropertyCacheKeyGenerator.GetCacheKey(metadata);

        // Check if already cached
        if (testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments.ContainsKey(cacheKey))
        {
            return;
        }

        // Resolve the property value from the data source
        var resolvedValue = await ResolvePropertyDataAsync(
            PropertyInitializationContext.ForCaching(metadata, objectBag, methodMetadata, events, testContext),
            cancellationToken);

        if (resolvedValue != null)
        {
            // Cache the resolved value
            ((ConcurrentDictionary<string, object?>)testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments)
                .TryAdd(cacheKey, resolvedValue);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT")]
    private Task ResolveAndCacheReflectionPropertiesAsync(
        (PropertyInfo Property, IDataSourceAttribute DataSource)[] properties,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        TestContext testContext,
        CancellationToken cancellationToken)
    {
        return ParallelTaskHelper.ForEachAsync(properties,
            pair => ResolveAndCacheReflectionPropertyAsync(pair.Property, pair.DataSource, objectBag, methodMetadata, events, testContext, cancellationToken));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT")]
    private async Task ResolveAndCacheReflectionPropertyAsync(
        PropertyInfo property,
        IDataSourceAttribute dataSource,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        TestContext testContext,
        CancellationToken cancellationToken)
    {
        var cacheKey = PropertyCacheKeyGenerator.GetCacheKey(property);

        // Check if already cached
        if (testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments.ContainsKey(cacheKey))
        {
            return;
        }

        var propertySetter = PropertySetterFactory.CreateSetter(property);

        var resolvedValue = await ResolvePropertyDataAsync(
            PropertyInitializationContext.ForReflectionCaching(property, dataSource, propertySetter, objectBag, methodMetadata, events, testContext),
            cancellationToken);

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
    private async Task<object?> ResolvePropertyDataAsync(PropertyInitializationContext context, CancellationToken cancellationToken = default)
    {
        var dataSource = await GetInitializedDataSourceAsync(context, cancellationToken);
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
                // EnsureInitializedAsync handles property injection and initialization.
                // ObjectInitializer is phase-aware: during Discovery phase, only IAsyncDiscoveryInitializer
                // objects are initialized; regular IAsyncInitializer objects are deferred to Execution phase.
                await _initializationCallback.Value.EnsureInitializedAsync(
                    value,
                    context.ObjectBag,
                    context.MethodMetadata,
                    context.Events,
                    cancellationToken);

                return value;
            }
        }

        return null;
    }

    private async Task<IDataSourceAttribute?> GetInitializedDataSourceAsync(PropertyInitializationContext context, CancellationToken cancellationToken = default)
    {
        IDataSourceAttribute? dataSource = context.DataSource ?? context.SourceGeneratedMetadata?.CreateDataSource();

        if (dataSource == null)
        {
            return null;
        }

        // Ensure the data source is initialized
        return await _initializationCallback.Value.EnsureInitializedAsync(
            dataSource,
            context.ObjectBag,
            context.MethodMetadata,
            context.Events,
            cancellationToken);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Metadata creation handles both modes")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "ContainingType and PropertyType are preserved through source generation")]
    private DataGeneratorMetadata CreateDataGeneratorMetadata(
        PropertyInitializationContext context,
        IDataSourceAttribute dataSource)
    {
        if (context.SourceGeneratedMetadata != null)
        {
            return CreateSourceGeneratedDataGeneratorMetadata(context, dataSource);
        }

        if (context.PropertyInfo != null)
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

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Metadata creation handles both modes")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "ContainingType and PropertyType are preserved through source generation")]
    private DataGeneratorMetadata CreateSourceGeneratedDataGeneratorMetadata(
        PropertyInitializationContext context,
        IDataSourceAttribute dataSource)
    {
        var metadata = context.SourceGeneratedMetadata!;

        if (metadata.ContainingType == null)
        {
            throw new InvalidOperationException(
                $"ContainingType is null for property '{context.PropertyName}'.");
        }

        // Cache the PropertyInfo lookup to avoid repeated reflection
        var propertyInfo = PropertyHelper.GetPropertyInfo(metadata.ContainingType, context.PropertyName);

        var propertyMetadata = new PropertyMetadata
        {
            IsStatic = false,
            Name = context.PropertyName,
            ClassMetadata = ClassMetadataHelper.GetOrCreateClassMetadata(metadata.ContainingType),
            Type = context.PropertyType,
            ReflectionInfo = propertyInfo,
            Getter = parent => propertyInfo.GetValue(parent!)!,
            ContainingTypeMetadata = ClassMetadataHelper.GetOrCreateClassMetadata(metadata.ContainingType)
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

    /// <summary>
    /// Gets a cached PropertyInfo for the given type and property name, avoiding repeated reflection calls.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "Source-gen properties have their types preserved at compile time")]
    private static PropertyInfo? GetCachedPropertyInfo(Type containingType, string propertyName)
    {
        return _propertyInfoCache.GetOrAdd((containingType, propertyName), key => key.Item1.GetProperty(key.Item2));
    }

    /// <summary>
    /// Rents a visited objects dictionary from the pool or creates a new one.
    /// </summary>
    private static ConcurrentDictionary<object, byte> RentVisitedDictionary()
    {
        if (_visitedObjectsPool.TryTake(out var visitedObjects))
        {
            return visitedObjects;
        }

#if NETSTANDARD2_0
        return new ConcurrentDictionary<object, byte>();
#else
        return new ConcurrentDictionary<object, byte>(Core.Helpers.ReferenceEqualityComparer.Instance);
#endif
    }

    /// <summary>
    /// Returns a visited objects dictionary to the pool for reuse.
    /// </summary>
    private static void ReturnVisitedDictionary(ConcurrentDictionary<object, byte> visitedObjects)
    {
        visitedObjects.Clear();
        _visitedObjectsPool.Add(visitedObjects);
    }
}
