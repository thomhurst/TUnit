using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyInjection;
using TUnit.Core.PropertyInjection.Initialization;
using TUnit.Core.Tracking;

namespace TUnit.Engine.Services;

/// <summary>
/// Unified service for managing object lifecycle.
/// Orchestrates: registration, property injection, initialization (IAsyncInitializer), and tracking.
/// Replaces the fragmented: PropertyInjectionService, DataSourceInitializer, PropertyInitializationOrchestrator,
/// PropertyDataResolver, and ObjectRegistrationService.
///
/// Uses Lazy&lt;T&gt; for dependencies to break circular references without manual Initialize() calls.
/// Follows clear phase separation: Register → Inject → Initialize → Cleanup.
/// </summary>
internal sealed class ObjectLifecycleService : IObjectRegistry
{
    private readonly Lazy<PropertyInjector> _propertyInjector;
    private readonly ObjectGraphDiscoveryService _objectGraphDiscoveryService;
    private readonly ObjectTracker _objectTracker;

    // Track initialization state per object
    private readonly ConcurrentDictionary<object, TaskCompletionSource<bool>> _initializationTasks = new();

    public ObjectLifecycleService(
        Lazy<PropertyInjector> propertyInjector,
        ObjectGraphDiscoveryService objectGraphDiscoveryService,
        ObjectTracker objectTracker)
    {
        _propertyInjector = propertyInjector;
        _objectGraphDiscoveryService = objectGraphDiscoveryService;
        _objectTracker = objectTracker;
    }

    private PropertyInjector PropertyInjector => _propertyInjector.Value;

    #region Phase 1: Registration (Discovery Time)

    /// <summary>
    /// Registers a test for lifecycle management during discovery.
    /// Injects properties so shared objects can be properly tracked across all tests.
    /// Does NOT call IAsyncInitializer (deferred to execution).
    /// </summary>
    public async Task RegisterTestAsync(TestContext testContext)
    {
        var testClassInstance = testContext.Metadata.TestDetails.ClassInstance;
        var objectBag = testContext.StateBag.Items;
        var methodMetadata = testContext.Metadata.TestDetails.MethodMetadata;
        var events = testContext.InternalEvents;

        // Inject properties during registration so shared objects are created early
        await PropertyInjector.InjectPropertiesAsync(testClassInstance, objectBag, methodMetadata, events);

        // Track the objects that need tracking for disposal
        _objectTracker.TrackObjects(testContext);
    }

    /// <summary>
    /// IObjectRegistry implementation - registers a single object.
    /// Injects properties but does NOT call IAsyncInitializer (deferred to execution).
    /// </summary>
    public async Task RegisterObjectAsync(
        object instance,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        // Inject properties during registration
        await PropertyInjector.InjectPropertiesAsync(instance, objectBag, methodMetadata, events);
    }

    /// <summary>
    /// IObjectRegistry implementation - registers multiple argument objects.
    /// </summary>
    public async Task RegisterArgumentsAsync(
        object?[] arguments,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events)
    {
        if (arguments == null || arguments.Length == 0)
        {
            return;
        }

        var tasks = new List<Task>();
        foreach (var argument in arguments)
        {
            if (argument != null)
            {
                tasks.Add(RegisterObjectAsync(argument, objectBag, methodMetadata, events));
            }
        }

        await Task.WhenAll(tasks);
    }

    #endregion

    #region Phase 2: Preparation (Execution Time)

    /// <summary>
    /// Prepares a test for execution.
    /// Sets already-resolved cached property values on the current instance and initializes tracked objects.
    /// This is needed because retries create new instances that don't have properties set yet.
    /// </summary>
    public async Task PrepareTestAsync(TestContext testContext, CancellationToken cancellationToken)
    {
        var testClassInstance = testContext.Metadata.TestDetails.ClassInstance;

        // Set already-cached property values on the current instance
        // This ensures new instances (from retries) get the same shared objects that were resolved during registration
        // We don't re-resolve from data sources - we just set the cached values
        SetCachedPropertiesOnInstance(testClassInstance, testContext);

        // Update tracking to ensure the current instance's object graph is tracked
        // TrackObjects has deduplication logic - shared objects already tracked won't be tracked again
        // This ensures that objects reachable from the current instance are properly tracked for disposal
        _objectTracker.TrackObjects(testContext);

        // Initialize all tracked objects (IAsyncInitializer) depth-first
        await InitializeTrackedObjectsAsync(testContext, cancellationToken);
    }

    /// <summary>
    /// Sets already-cached property values on a test class instance.
    /// This is used to apply cached property values to new instances created during retries.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT")]
    private void SetCachedPropertiesOnInstance(object instance, TestContext testContext)
    {
        var plan = PropertyInjectionCache.GetOrCreatePlan(instance.GetType());

        if (!plan.HasProperties)
        {
            return;
        }

        var cachedProperties = testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments;

        if (plan.SourceGeneratedProperties.Length > 0)
        {
            foreach (var metadata in plan.SourceGeneratedProperties)
            {
                var cacheKey = $"{metadata.ContainingType.FullName}.{metadata.PropertyName}";

                if (cachedProperties.TryGetValue(cacheKey, out var cachedValue) && cachedValue != null)
                {
                    // Set the cached value on the new instance
                    metadata.SetProperty(instance, cachedValue);
                }
            }
        }
        else if (plan.ReflectionProperties.Length > 0)
        {
            foreach (var (property, _) in plan.ReflectionProperties)
            {
                var cacheKey = $"{property.DeclaringType!.FullName}.{property.Name}";

                if (cachedProperties.TryGetValue(cacheKey, out var cachedValue) && cachedValue != null)
                {
                    // Set the cached value on the new instance
                    var setter = PropertySetterFactory.CreateSetter(property);
                    setter(instance, cachedValue);
                }
            }
        }
    }

    /// <summary>
    /// Initializes all tracked objects depth-first (deepest objects first).
    /// </summary>
    private async Task InitializeTrackedObjectsAsync(TestContext testContext, CancellationToken cancellationToken)
    {
        var levels = testContext.TrackedObjects.Keys.OrderByDescending(level => level);

        foreach (var level in levels)
        {
            var objectsAtLevel = testContext.TrackedObjects[level];

            // Initialize all objects at this depth in parallel
            await Task.WhenAll(objectsAtLevel.Select(obj =>
                EnsureInitializedAsync(
                    obj,
                    testContext.StateBag.Items,
                    testContext.Metadata.TestDetails.MethodMetadata,
                    testContext.InternalEvents,
                    cancellationToken).AsTask()));
        }

        // Finally initialize the test class itself
        await EnsureInitializedAsync(
            testContext.Metadata.TestDetails.ClassInstance,
            testContext.StateBag.Items,
            testContext.Metadata.TestDetails.MethodMetadata,
            testContext.InternalEvents,
            cancellationToken);
    }

    #endregion

    #region Phase 3: Object Initialization

    /// <summary>
    /// Ensures an object is fully initialized (property injection + IAsyncInitializer).
    /// Thread-safe with fast-path for already-initialized objects.
    /// </summary>
    public async ValueTask<T> EnsureInitializedAsync<T>(
        T obj,
        ConcurrentDictionary<string, object?>? objectBag = null,
        MethodMetadata? methodMetadata = null,
        TestContextEvents? events = null,
        CancellationToken cancellationToken = default) where T : notnull
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        // Fast path: already initialized
        if (_initializationTasks.TryGetValue(obj, out var existingTcs) && existingTcs.Task.IsCompleted)
        {
            if (existingTcs.Task.IsFaulted)
            {
                await existingTcs.Task.ConfigureAwait(false);
            }
            return obj;
        }

        // Slow path: need to initialize
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        existingTcs = _initializationTasks.GetOrAdd(obj, tcs);

        if (existingTcs == tcs)
        {
            try
            {
                await InitializeObjectCoreAsync(obj, objectBag, methodMetadata, events, cancellationToken);
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
            await existingTcs.Task.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        return obj;
    }

    /// <summary>
    /// Core initialization: property injection + nested objects + IAsyncInitializer.
    /// </summary>
    private async Task InitializeObjectCoreAsync(
        object obj,
        ConcurrentDictionary<string, object?>? objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents? events,
        CancellationToken cancellationToken)
    {
        objectBag ??= new ConcurrentDictionary<string, object?>();
        events ??= new TestContextEvents();

        try
        {
            // Step 1: Inject properties
            await PropertyInjector.InjectPropertiesAsync(obj, objectBag, methodMetadata, events);

            // Step 2: Initialize nested objects depth-first
            await InitializeNestedObjectsAsync(obj, cancellationToken);

            // Step 3: Call IAsyncInitializer on the object itself
            if (obj is IAsyncInitializer asyncInitializer)
            {
                await ObjectInitializer.InitializeAsync(asyncInitializer, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to initialize object of type '{obj.GetType().Name}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Initializes nested objects depth-first using the centralized ObjectGraphDiscoveryService.
    /// </summary>
    private async Task InitializeNestedObjectsAsync(object rootObject, CancellationToken cancellationToken)
    {
        var graph = _objectGraphDiscoveryService.DiscoverNestedObjectGraph(rootObject);

        // Initialize from deepest to shallowest (skip depth 0 which is the root itself)
        foreach (var depth in graph.GetDepthsDescending())
        {
            if (depth == 0) continue; // Root handled separately

            var objectsAtDepth = graph.GetObjectsAtDepth(depth);

            await Task.WhenAll(objectsAtDepth
                .Where(obj => obj is IAsyncInitializer)
                .Select(obj => ObjectInitializer.InitializeAsync(obj, cancellationToken).AsTask()));
        }
    }

    #endregion

    #region Phase 4: Cleanup

    /// <summary>
    /// Cleans up after test execution.
    /// Decrements reference counts and disposes objects when count reaches zero.
    /// </summary>
    public async Task CleanupTestAsync(TestContext testContext, List<Exception> cleanupExceptions)
    {
        await _objectTracker.UntrackObjects(testContext, cleanupExceptions);
    }

    #endregion

    /// <summary>
    /// Clears the initialization cache. Called at end of test session.
    /// </summary>
    public void ClearCache()
    {
        _initializationTasks.Clear();
    }
}
