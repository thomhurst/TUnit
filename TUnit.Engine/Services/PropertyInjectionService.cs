using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.PropertyInjection;

namespace TUnit.Engine.Services;

/// <summary>
/// Internal service for property injection.
/// Used by ObjectRegistrationService during registration phase.
/// </summary>
internal sealed class PropertyInjectionService
{
    private readonly DataSourceInitializer _dataSourceInitializer;
    private PropertyInitializationOrchestrator _orchestrator;

    // Simple object pool for visited objects dictionaries to reduce allocations
    private static readonly ConcurrentBag<ConcurrentDictionary<object, byte>> _visitedObjectsPool = new();

    public PropertyInjectionService(DataSourceInitializer dataSourceInitializer)
    {
        _dataSourceInitializer = dataSourceInitializer ?? throw new ArgumentNullException(nameof(dataSourceInitializer));
        _orchestrator = new PropertyInitializationOrchestrator(dataSourceInitializer, null!);
    }

    /// <summary>
    /// Completes initialization by providing the ObjectRegistrationService as IObjectRegistry.
    /// This two-phase initialization breaks the circular dependency while maintaining type safety.
    /// </summary>
    public void Initialize(IObjectRegistry objectRegistry)
    {
        _orchestrator = new PropertyInitializationOrchestrator(_dataSourceInitializer, objectRegistry);
    }

    /// <summary>
    /// Injects properties with data sources into argument objects just before test execution.
    /// This ensures properties are only initialized when the test is about to run.
    /// Arguments are processed in parallel for better performance.
    /// </summary>
    public async Task InjectPropertiesIntoArgumentsAsync(object?[] arguments, ConcurrentDictionary<string, object?> objectBag, MethodMetadata methodMetadata, TestContextEvents events)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        var injectableArgs = arguments
            .Where(argument => argument != null && PropertyInjectionCache.HasInjectableProperties(argument.GetType()))
            .ToArray();

        if (injectableArgs.Length == 0)
        {
            return;
        }

        var argumentTasks = injectableArgs
            .Select(argument => InjectPropertiesIntoObjectAsync(argument!, objectBag, methodMetadata, events))
            .ToArray();

        await Task.WhenAll(argumentTasks);
    }


    /// <summary>
    /// Recursively injects properties with data sources into a single object.
    /// Uses source generation mode when available, falls back to reflection mode.
    /// After injection, handles tracking, initialization, and recursive injection.
    /// </summary>
    /// <param name="instance">The object instance to inject properties into.</param>
    /// <param name="objectBag">Shared object bag for the test context. Must not be null.</param>
    /// <param name="methodMetadata">Method metadata for the test. Can be null.</param>
    /// <param name="events">Test context events for tracking. Must not be null and must be unique per test permutation.</param>
    public async Task InjectPropertiesIntoObjectAsync(object instance, ConcurrentDictionary<string, object?> objectBag, MethodMetadata? methodMetadata, TestContextEvents events)
    {
        if (objectBag == null)
        {
            throw new ArgumentNullException(nameof(objectBag));
        }

        if (events == null)
        {
            throw new ArgumentNullException(nameof(events), "TestContextEvents must not be null. Each test permutation must have a unique TestContextEvents instance for proper disposal tracking.");
        }

        // Rent dictionary from pool to avoid allocations
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
            await InjectPropertiesIntoObjectAsyncCore(instance, objectBag, methodMetadata, events, visitedObjects);
        }
        finally
        {
            // Clear and return to pool (reject if too large to avoid memory bloat)
            visitedObjects.Clear();
            if (visitedObjects.Count == 0)
            {
                _visitedObjectsPool.Add(visitedObjects);
            }
        }
    }

    internal async Task InjectPropertiesIntoObjectAsyncCore(object instance, ConcurrentDictionary<string, object?> objectBag, MethodMetadata? methodMetadata, TestContextEvents events, ConcurrentDictionary<object, byte> visitedObjects)
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
            // Use optimized single-lookup pattern with fast-path for already-injected instances
            await PropertyInjectionCache.EnsureInjectedAsync(instance, async _ =>
            {
                var plan = PropertyInjectionCache.GetOrCreatePlan(instance.GetType());

                await _orchestrator.InitializeObjectWithPropertiesAsync(
                    instance, plan, objectBag, methodMetadata, events, visitedObjects);
            });

            await RecurseIntoNestedPropertiesAsync(instance, objectBag, methodMetadata, events, visitedObjects);
        }
        catch (Exception ex)
        {
            var detailedMessage = $"Failed to inject properties for type '{instance.GetType().Name}': {ex.Message}";

            if (ex.StackTrace != null)
            {
                detailedMessage += $"\nStack trace: {ex.StackTrace}";
            }

            throw new InvalidOperationException(detailedMessage, ex);
        }
    }

    /// <summary>
    /// Recursively injects properties into nested objects that have injectable properties.
    /// This is called after the direct properties of an object have been initialized.
    /// </summary>
    private async Task RecurseIntoNestedPropertiesAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        ConcurrentDictionary<object, byte> visitedObjects)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(instance.GetType());
        if (!plan.HasProperties)
        {
            return;
        }

        // Use whichever properties are available in the plan
        // For closed generic types, source-gen may not have registered them, so use reflection fallback
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
                    await InjectPropertiesIntoObjectAsyncCore(propertyValue, objectBag, methodMetadata, events, visitedObjects);
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
                    await InjectPropertiesIntoObjectAsyncCore(propertyValue, objectBag, methodMetadata, events, visitedObjects);
                }
            }
        }
    }
}
