using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyInjection;

namespace TUnit.Engine.Services;

/// <summary>
/// Centralized service responsible for initializing data source instances.
/// Ensures all data sources are properly initialized before use, regardless of where they're used
/// (properties, constructor arguments, or method arguments).
/// </summary>
internal sealed class DataSourceInitializer
{
    private readonly Dictionary<object, Task> _initializationTasks = new();
    private readonly object _lock = new();
    private PropertyInjectionService? _propertyInjectionService;

    public void Initialize(PropertyInjectionService propertyInjectionService)
    {
        _propertyInjectionService = propertyInjectionService;
    }

    /// <summary>
    /// Ensures a data source instance is fully initialized before use.
    /// This includes property injection and calling IAsyncInitializer if implemented.
    /// </summary>
    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
    #endif
    public async Task<T> EnsureInitializedAsync<T>(
        T dataSource,
        Dictionary<string, object?>? objectBag = null,
        MethodMetadata? methodMetadata = null,
        TestContextEvents? events = null) where T : notnull
    {
        if (dataSource == null)
        {
            throw new ArgumentNullException(nameof(dataSource));
        }

        // Check if already initialized or being initialized
        Task? existingTask;
        lock (_lock)
        {
            if (_initializationTasks.TryGetValue(dataSource, out existingTask))
            {
                // Already initialized or being initialized
            }
            else
            {
                // Start initialization
                existingTask = InitializeDataSourceAsync(dataSource, objectBag, methodMetadata, events);
                _initializationTasks[dataSource] = existingTask;
            }
        }

        await existingTask;
        return dataSource;
    }

    /// <summary>
    /// Initializes a data source instance with the complete lifecycle.
    /// </summary>
    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
    #endif
    private async Task InitializeDataSourceAsync(
        object dataSource,
        Dictionary<string, object?>? objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents? events)
    {
        try
        {
            // Ensure we have required context
            objectBag ??= new Dictionary<string, object?>();
            events ??= new TestContextEvents();

            // Initialize the data source directly here
            // Step 1: Property injection - use PropertyInjectionService if available
            if (_propertyInjectionService != null && PropertyInjectionCache.HasInjectableProperties(dataSource.GetType()))
            {
                await _propertyInjectionService.InjectPropertiesIntoObjectAsync(
                    dataSource, objectBag, methodMetadata, events);
            }

            // Step 2: Initialize nested property-injected objects (deepest first)
            // This ensures that when the parent's IAsyncInitializer runs, all nested objects are already initialized
            await InitializeNestedObjectsAsync(dataSource);

            // Step 3: IAsyncInitializer on the data source itself
            if (dataSource is IAsyncInitializer asyncInitializer)
            {
                await ObjectInitializer.InitializeAsync(asyncInitializer);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to initialize data source of type '{dataSource.GetType().Name}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Initializes all nested property-injected objects in depth-first order.
    /// This ensures that when the parent's IAsyncInitializer runs, all nested dependencies are already initialized.
    /// </summary>
    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
    #endif
    private async Task InitializeNestedObjectsAsync(object rootObject)
    {
        var objectsByDepth = new Dictionary<int, HashSet<object>>();
        var visitedObjects = new HashSet<object>();

        // Collect all nested property-injected objects grouped by depth
        CollectNestedObjects(rootObject, objectsByDepth, visitedObjects, currentDepth: 1);

        // Initialize objects deepest-first (highest depth to lowest)
        var depths = objectsByDepth.Keys.OrderByDescending(depth => depth);

        foreach (var depth in depths)
        {
            var objectsAtDepth = objectsByDepth[depth];

            // Initialize all objects at this depth in parallel
            await Task.WhenAll(objectsAtDepth.Select(obj => ObjectInitializer.InitializeAsync(obj).AsTask()));
        }
    }

    /// <summary>
    /// Recursively collects all nested property-injected objects grouped by depth.
    /// </summary>
    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
    #endif
    private void CollectNestedObjects(
        object obj,
        Dictionary<int, HashSet<object>> objectsByDepth,
        HashSet<object> visitedObjects,
        int currentDepth)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(obj.GetType());

        if (!SourceRegistrar.IsEnabled)
        {
            // Reflection mode
            foreach (var prop in plan.ReflectionProperties)
            {
                var value = prop.Property.GetValue(obj);

                if (value == null || !visitedObjects.Add(value))
                {
                    continue;
                }

                // Add to the current depth level if it has injectable properties or implements IAsyncInitializer
                if (PropertyInjectionCache.HasInjectableProperties(value.GetType()) || value is IAsyncInitializer)
                {
                    if (!objectsByDepth.ContainsKey(currentDepth))
                    {
                        objectsByDepth[currentDepth] = [];
                    }

                    objectsByDepth[currentDepth].Add(value);
                }

                // Recursively collect nested objects
                if (PropertyInjectionCache.HasInjectableProperties(value.GetType()))
                {
                    CollectNestedObjects(value, objectsByDepth, visitedObjects, currentDepth + 1);
                }
            }
        }
        else
        {
            // Source-generated mode
            foreach (var metadata in plan.SourceGeneratedProperties)
            {
                var property = metadata.ContainingType.GetProperty(metadata.PropertyName);

                if (property == null || !property.CanRead)
                {
                    continue;
                }

                var value = property.GetValue(obj);

                if (value == null || !visitedObjects.Add(value))
                {
                    continue;
                }

                // Add to the current depth level if it has injectable properties or implements IAsyncInitializer
                if (PropertyInjectionCache.HasInjectableProperties(value.GetType()) || value is IAsyncInitializer)
                {
                    if (!objectsByDepth.ContainsKey(currentDepth))
                    {
                        objectsByDepth[currentDepth] = [];
                    }

                    objectsByDepth[currentDepth].Add(value);
                }

                // Recursively collect nested objects
                if (PropertyInjectionCache.HasInjectableProperties(value.GetType()))
                {
                    CollectNestedObjects(value, objectsByDepth, visitedObjects, currentDepth + 1);
                }
            }
        }
    }

    /// <summary>
    /// Clears the initialization cache. Should be called at the end of test sessions.
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            _initializationTasks.Clear();
        }
    }
}
