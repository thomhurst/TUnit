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
    private readonly ConcurrentDictionary<object, TaskCompletionSource<bool>> _initializationTasks = new();
    private PropertyInjectionService? _propertyInjectionService;

    /// <summary>
    /// Completes initialization by providing the PropertyInjectionService.
    /// This two-phase initialization breaks the circular dependency.
    /// </summary>
    public void Initialize(PropertyInjectionService propertyInjectionService)
    {
        _propertyInjectionService = propertyInjectionService ?? throw new ArgumentNullException(nameof(propertyInjectionService));
    }

    /// <summary>
    /// Ensures a data source instance is fully initialized before use.
    /// This includes property injection and calling IAsyncInitializer if implemented.
    /// Optimized with fast-path for already-initialized data sources.
    /// </summary>
    public async ValueTask<T> EnsureInitializedAsync<T>(
        T dataSource,
        ConcurrentDictionary<string, object?>? objectBag = null,
        MethodMetadata? methodMetadata = null,
        TestContextEvents? events = null,
        CancellationToken cancellationToken = default) where T : notnull
    {
        if (dataSource == null)
        {
            throw new ArgumentNullException(nameof(dataSource));
        }

        // Fast path: Check if already initialized (avoids Task allocation)
        if (_initializationTasks.TryGetValue(dataSource, out var existingTcs) && existingTcs.Task.IsCompleted)
        {
            // Already initialized - return synchronously without allocating a Task
            if (existingTcs.Task.IsFaulted)
            {
                // Re-throw the original exception
                await existingTcs.Task.ConfigureAwait(false);
            }

            return dataSource;
        }

        // Slow path: Need to initialize or wait for initialization
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        existingTcs = _initializationTasks.GetOrAdd(dataSource, tcs);

        if (existingTcs == tcs)
        {
            try
            {
                await InitializeDataSourceAsync(dataSource, objectBag, methodMetadata, events, cancellationToken).ConfigureAwait(false);
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                throw;
            }
        }
        else
        {
            // Another thread is initializing or already initialized - wait for it
            await existingTcs.Task.ConfigureAwait(false);

            // Check cancellation after waiting
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        return dataSource;
    }

    /// <summary>
    /// Initializes a data source instance with the complete lifecycle.
    /// </summary>
    private async Task InitializeDataSourceAsync(
        object dataSource,
        ConcurrentDictionary<string, object?>? objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents? events,
        CancellationToken cancellationToken)
    {
        try
        {
            // Ensure we have required context
            objectBag ??= new ConcurrentDictionary<string, object?>();
            events ??= new TestContextEvents();

            // Initialize the data source directly here
            // Step 1: Property injection (if PropertyInjectionService has been initialized)
            if (_propertyInjectionService != null && PropertyInjectionCache.HasInjectableProperties(dataSource.GetType()))
            {
                await _propertyInjectionService.InjectPropertiesIntoObjectAsync(
                    dataSource, objectBag, methodMetadata, events);
            }

            // Step 2: Initialize nested property-injected objects (deepest first)
            // This ensures that when the parent's IAsyncInitializer runs, all nested objects are already initialized
            await InitializeNestedObjectsAsync(dataSource, cancellationToken);

            // Step 3: IAsyncInitializer on the data source itself
            if (dataSource is IAsyncInitializer asyncInitializer)
            {
                await ObjectInitializer.InitializeAsync(asyncInitializer, cancellationToken);
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
    private async Task InitializeNestedObjectsAsync(object rootObject, CancellationToken cancellationToken)
    {
        var objectsByDepth = new Dictionary<int, HashSet<object>>(capacity: 4);
        var visitedObjects = new HashSet<object>();

        // Collect all nested property-injected objects grouped by depth
        CollectNestedObjects(rootObject, objectsByDepth, visitedObjects, currentDepth: 1);

        // Initialize objects deepest-first (highest depth to lowest)
        var depths = objectsByDepth.Keys.OrderByDescending(depth => depth);

        foreach (var depth in depths)
        {
            var objectsAtDepth = objectsByDepth[depth];

            // Initialize all objects at this depth in parallel
            await Task.WhenAll(objectsAtDepth.Select(obj => ObjectInitializer.InitializeAsync(obj, cancellationToken).AsTask()));
        }
    }

    /// <summary>
    /// Recursively collects all nested property-injected objects grouped by depth.
    /// </summary>
    private void CollectNestedObjects(
        object obj,
        Dictionary<int, HashSet<object>> objectsByDepth,
        HashSet<object> visitedObjects,
        int currentDepth)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(obj.GetType());

        // Use whichever properties are available in the plan
        // For closed generic types, source-gen may not have registered them, so use reflection fallback
        if (plan.SourceGeneratedProperties.Length > 0)
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
        else if (plan.ReflectionProperties.Length > 0)
        {
            // Reflection mode fallback
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
    }

    /// <summary>
    /// Clears the initialization cache. Should be called at the end of test sessions.
    /// </summary>
    public void ClearCache()
    {
        _initializationTasks.Clear();
    }
}
