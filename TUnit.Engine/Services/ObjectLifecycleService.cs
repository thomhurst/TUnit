using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyInjection;
using TUnit.Core.Tracking;
#if NET
using System.Diagnostics;
#endif

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
/// <remarks>
/// Implements <see cref="IInitializationCallback"/> to allow PropertyInjector to call back for initialization
/// without creating a direct dependency (breaking the circular reference pattern).
/// </remarks>
internal sealed class ObjectLifecycleService : IObjectRegistry, IInitializationCallback
{
    private readonly Lazy<PropertyInjector> _propertyInjector;
    private readonly ObjectGraphDiscoveryService _objectGraphDiscoveryService;
    private readonly ObjectTracker _objectTracker;

    // Track initialization state per object
    // Use ReferenceEqualityComparer to prevent objects with custom Equals from sharing initialization state
    private readonly ConcurrentDictionary<object, TaskCompletionSource<bool>> _initializationTasks =
        new(Core.Helpers.ReferenceEqualityComparer.Instance);

#if NET
    // Gates span creation so only the first caller for a given object creates a trace span.
    // Subsequent callers (concurrent tests sharing the same object) skip span creation
    // and just await ObjectInitializer's deduplicated Lazy<Task>.
    private readonly ConcurrentDictionary<object, byte> _spannedObjects =
        new(Core.Helpers.ReferenceEqualityComparer.Instance);
#endif

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
    /// Resolves and caches property values (to create shared objects early) without setting them on the placeholder instance.
    /// Tracks the resolved objects so reference counting works correctly across all tests.
    /// Does NOT call IAsyncInitializer (deferred to execution).
    /// </summary>
    public async Task RegisterTestAsync(TestContext testContext, CancellationToken cancellationToken = default)
    {
        var objectBag = testContext.StateBag.Items;
        var methodMetadata = testContext.Metadata.TestDetails.MethodMetadata;
        var events = testContext.InternalEvents;
        var testClassType = testContext.Metadata.TestDetails.ClassType;

        // Resolve property values (creating shared objects) and cache them WITHOUT setting on placeholder instance
        // This ensures shared objects are created once and tracked with the correct reference count
        await PropertyInjector.ResolveAndCachePropertiesAsync(testClassType, objectBag, methodMetadata, events, testContext, cancellationToken);

        // Track the cached objects so they get the correct reference count
        _objectTracker.TrackObjects(testContext);
    }

    /// <summary>
    /// IObjectRegistry implementation - registers a single object.
    /// Injects properties but does NOT call IAsyncInitializer (deferred to execution).
    /// </summary>
    public Task RegisterObjectAsync(
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

        // Inject properties during registration
        return PropertyInjector.InjectPropertiesAsync(instance, objectBag, methodMetadata, events, cancellationToken);
    }

    /// <summary>
    /// IObjectRegistry implementation - registers multiple argument objects.
    /// </summary>
    public Task RegisterArgumentsAsync(
        object?[] arguments,
        ConcurrentDictionary<string, object?> objectBag,
        MethodMetadata? methodMetadata,
        TestContextEvents events,
        CancellationToken cancellationToken = default)
    {
        if (arguments == null || arguments.Length == 0)
        {
            return Task.CompletedTask;
        }

        // Pre-allocate with expected capacity to avoid resizing
        var tasks = new ValueListBuilder<Task>(arguments.Length);
        foreach (var argument in arguments)
        {
            if (argument != null)
            {
                tasks.Append(RegisterObjectAsync(argument, objectBag, methodMetadata, events, cancellationToken));
            }
        }

#if NET9_0_OR_GREATER
        var returnTask = Task.WhenAll(tasks.AsSpan());
#else
        var returnTask = tasks.Length == 1
            ? tasks[0]
            : Task.WhenAll(tasks.AsSpan().ToArray());
#endif
        tasks.Dispose();
        return returnTask;
    }

    #endregion

    #region Phase 2: Preparation (Execution Time)

    /// <summary>
    /// Prepares a test for execution.
    /// Sets already-resolved cached property values on the current instance.
    /// This is needed because retries create new instances that don't have properties set yet.
    /// Does NOT call IAsyncInitializer - that is deferred until after BeforeClass hooks via InitializeTestObjectsAsync.
    /// </summary>
    public void PrepareTest(TestContext testContext)
    {
        var testClassInstance = testContext.Metadata.TestDetails.ClassInstance;

        // Set already-cached property values on the current instance
        // Properties were resolved and cached during RegisterTestAsync, so shared objects are already created
        // We just need to set them on the actual test instance (retries create new instances)
        SetCachedPropertiesOnInstance(testClassInstance, testContext);
    }

    /// <summary>
    /// Initializes test objects (IAsyncInitializer) after BeforeClass hooks have run.
    /// This ensures resources like Docker containers are not started until needed.
    /// </summary>
    public Task InitializeTestObjectsAsync(TestContext testContext, CancellationToken cancellationToken)
    {
        // Initialize all tracked objects (IAsyncInitializer) depth-first
        return InitializeTrackedObjectsAsync(testContext, cancellationToken);
    }

    /// <summary>
    /// Initializes an object and its nested IAsyncInitializer objects for execution.
    /// Used to initialize data source objects before they are passed to the test constructor.
    /// </summary>
    public async Task InitializeObjectForExecutionAsync(object? obj, CancellationToken cancellationToken = default)
    {
        if (obj is null)
        {
            return;
        }

        await InitializeNestedObjectsForExecutionAsync(obj, cancellationToken);
        await ObjectInitializer.InitializeAsync(obj, cancellationToken);
    }

    /// <summary>
    /// Sets already-cached property values on a test class instance.
    /// This is used to apply cached property values to new instances created during retries.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection mode is not used in AOT")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "PropertyType is preserved through source generation or reflection discovery — annotation can't flow through PropertyInfo/PropertyInjectionMetadata")]
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
                var cacheKey = PropertyCacheKeyGenerator.GetCacheKey(metadata);

                if (cachedProperties.TryGetValue(cacheKey, out var cachedValue) && cachedValue != null)
                {
                    // Convert if needed — cached values may be unconverted when pre-resolved during registration
                    cachedValue = CastHelper.CastIfNeeded(metadata.PropertyType, cachedValue);
                    metadata.SetProperty(instance, cachedValue);
                }
            }
        }
        else if (plan.ReflectionProperties.Length > 0)
        {
            foreach (var (property, _) in plan.ReflectionProperties)
            {
                var cacheKey = PropertyCacheKeyGenerator.GetCacheKey(property);

                if (cachedProperties.TryGetValue(cacheKey, out var cachedValue) && cachedValue != null)
                {
                    // Convert if needed — cached values may be unconverted when pre-resolved during registration
                    cachedValue = CastHelper.CastIfNeeded(property.PropertyType, cachedValue);
                    var setter = PropertySetterFactory.CreateSetter(property);
                    setter(instance, cachedValue);
                }
            }
        }
    }

    /// <summary>
    /// Initializes all tracked objects depth-first (deepest objects first).
    /// This is called during test execution (after BeforeClass hooks) to initialize IAsyncInitializer objects.
    /// Objects at the same level are initialized in parallel.
    /// Each object gets its own OpenTelemetry span parented under the appropriate scope activity
    /// based on its <see cref="SharedType"/> (registered in <see cref="TraceScopeRegistry"/>).
    /// </summary>
    private async Task InitializeTrackedObjectsAsync(TestContext testContext, CancellationToken cancellationToken)
    {
        // SortedList keeps keys in ascending order; iterate by index in reverse for descending depth.
        var trackedObjects = testContext.TrackedObjects;
        var values = trackedObjects.Values;

        for (var i = values.Count - 1; i >= 0; i--)
        {
            var objectsAtLevel = values[i];

            // Initialize all objects at this level in parallel
            var tasks = new List<Task>(objectsAtLevel.Count);
            foreach (var obj in objectsAtLevel)
            {
                tasks.Add(InitializeObjectWithSpanAsync(obj, testContext, cancellationToken));
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        // Finally initialize the test class and its nested objects.
        // The test class instance is unregistered in TraceScopeRegistry, so
        // GetSharedType returns null → defaults to per-test scope automatically.
        var classInstance = testContext.Metadata.TestDetails.ClassInstance;
        await InitializeObjectWithSpanAsync(classInstance, testContext, cancellationToken);
    }

    /// <summary>
    /// Initializes an object and its nested objects, wrapped in a scope-aware OpenTelemetry span.
    /// </summary>
    private async Task InitializeObjectWithSpanAsync(object obj, TestContext testContext, CancellationToken cancellationToken)
    {
        // First initialize nested objects depth-first
        await InitializeNestedObjectsForExecutionAsync(obj, cancellationToken);

#if NET
        // Only the first caller for a given object creates a trace span.
        // TryAdd is atomic — exactly one concurrent caller wins the gate.
        // Subsequent callers skip span creation and go straight to ObjectInitializer,
        // which deduplicates via Lazy<Task>.
        if (obj is IAsyncInitializer && TUnitActivitySource.Source.HasListeners() && _spannedObjects.TryAdd(obj, 0))
        {
            var sharedType = TraceScopeRegistry.GetSharedType(obj);
            await InitializeWithSpanAsync(obj, testContext, sharedType, cancellationToken);
        }
        else
        {
            await ObjectInitializer.InitializeAsync(obj, cancellationToken);
        }
#else
        await ObjectInitializer.InitializeAsync(obj, cancellationToken);
#endif
    }

#if NET
    /// <summary>
    /// Initializes an object within an OpenTelemetry span parented under the appropriate scope activity.
    /// </summary>
    private static async Task InitializeWithSpanAsync(
        object obj,
        TestContext testContext,
        SharedType? sharedType,
        CancellationToken cancellationToken)
    {
        var parentContext = GetParentActivityContext(testContext, sharedType);

        await TUnitActivitySource.RunWithSpanAsync(
            $"initialize {obj.GetType().Name}",
            parentContext,
            [
                new(TUnitActivitySource.TagTestId, testContext.Id),
                new(TUnitActivitySource.TagTestClass, testContext.Metadata.TestDetails.ClassType.FullName),
                new(TUnitActivitySource.TagTraceScope, TUnitActivitySource.GetScopeTag(sharedType))
            ],
            () => ObjectInitializer.InitializeAsync(obj, cancellationToken).AsTask());
    }

    /// <summary>
    /// Returns the parent activity context for an initialization span.
    /// Session/keyed/assembly-scoped objects are parented under the assembly activity so they
    /// appear as siblings of suite spans in both OTel backends and the HTML timeline.
    /// The <c>tunit.trace.scope</c> tag carries the precise lifetime semantics.
    /// </summary>
    private static ActivityContext GetParentActivityContext(TestContext testContext, SharedType? sharedType)
    {
        return sharedType switch
        {
            SharedType.PerTestSession or SharedType.PerAssembly or SharedType.Keyed
                => testContext.ClassContext.AssemblyContext.Activity?.Context ?? default,
            SharedType.PerClass => testContext.ClassContext.Activity?.Context ?? default,
            // Per-test objects and the test class instance go under the test case activity
            _ => testContext.Activity?.Context ?? testContext.ClassContext.Activity?.Context ?? default
        };
    }
#endif

    /// <summary>
    /// Initializes nested objects during execution phase - all IAsyncInitializer objects.
    /// </summary>
    private Task InitializeNestedObjectsForExecutionAsync(object rootObject, CancellationToken cancellationToken)
    {
        return InitializeNestedObjectsAsync(
            rootObject,
            ObjectInitializer.InitializeAsync,
            cancellationToken);
    }

    #endregion

    #region Phase 3: Object Initialization

    /// <summary>
    /// Injects properties into an object without calling IAsyncInitializer.
    /// Used during test discovery to prepare data sources without triggering async initialization.
    /// </summary>
    public async ValueTask<T> InjectPropertiesAsync<T>(
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

        objectBag ??= new ConcurrentDictionary<string, object?>();
        events ??= new TestContextEvents();

        // Only inject properties, do not call IAsyncInitializer
        await PropertyInjector.InjectPropertiesAsync(obj, objectBag, methodMetadata, events, cancellationToken);

        return obj;
    }

    /// <summary>
    /// Ensures an object is fully initialized (property injection + IAsyncInitializer).
    /// Thread-safe with fast-path for already-initialized objects.
    /// Called during test execution to initialize all IAsyncInitializer objects.
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

        // Fast path: already processed by this service
        if (_initializationTasks.TryGetValue(obj, out var existingTcs) && existingTcs.Task.IsCompleted)
        {
            if (existingTcs.Task.IsFaulted || existingTcs.Task.IsCanceled)
            {
                await existingTcs.Task.ConfigureAwait(false);
            }

            // EnsureInitializedAsync is only called during discovery (from PropertyInjector).
            // If the object is shared and has already been processed, just return it.
            // Regular IAsyncInitializer objects will be initialized during execution via InitializeTrackedObjectsAsync.
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
            catch (OperationCanceledException)
            {
                // Do NOT remove from cache - the cancelled TCS stays so subsequent
                // callers get the cancellation immediately. Retrying can cause hangs
                // when InitializeAsync partially initialized resources (#4715).
                tcs.SetCanceled();
                throw;
            }
            catch (Exception ex)
            {
                // Do NOT remove from cache - the faulted TCS stays so subsequent
                // callers get the same error immediately. Retrying can cause hangs
                // when InitializeAsync partially initialized resources (#4715).
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
    /// Core initialization: property injection + IAsyncDiscoveryInitializer only.
    /// Regular IAsyncInitializer objects are NOT initialized here - they are deferred to execution phase.
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

        // Let exceptions propagate naturally - don't wrap in InvalidOperationException
        // This aligns with ObjectInitializer behavior and provides cleaner stack traces

        // Step 1: Inject properties
        await PropertyInjector.InjectPropertiesAsync(obj, objectBag, methodMetadata, events, cancellationToken);

        // Step 2: Initialize nested objects depth-first (discovery-only)
        await InitializeNestedObjectsForDiscoveryAsync(obj, cancellationToken);

        // Step 3: Call IAsyncDiscoveryInitializer only (not regular IAsyncInitializer)
        // Regular IAsyncInitializer objects are deferred to execution phase via InitializeTestObjectsAsync
        await ObjectInitializer.InitializeForDiscoveryAsync(obj, cancellationToken);
    }

    /// <summary>
    /// Initializes nested objects during discovery phase - only IAsyncDiscoveryInitializer objects.
    /// </summary>
    private Task InitializeNestedObjectsForDiscoveryAsync(object rootObject, CancellationToken cancellationToken)
    {
        return InitializeNestedObjectsAsync(
            rootObject,
            ObjectInitializer.InitializeForDiscoveryAsync,
            cancellationToken);
    }

    /// <summary>
    /// Shared implementation for nested object initialization (DRY).
    /// Discovers nested objects and initializes them depth-first using the provided initializer.
    /// </summary>
    /// <param name="rootObject">The root object to discover nested objects from.</param>
    /// <param name="initializer">The initializer function to call for each object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task InitializeNestedObjectsAsync(
        object rootObject,
        Func<object?, CancellationToken, ValueTask> initializer,
        CancellationToken cancellationToken)
    {
        var graph = _objectGraphDiscoveryService.DiscoverNestedObjectGraph(rootObject, cancellationToken);

        // Initialize from deepest to shallowest (skip depth 0 which is the root itself)
        foreach (var depth in graph.GetDepthsDescending())
        {
            if (depth == 0)
            {
                continue; // Root handled separately
            }

            var objectsAtDepth = graph.GetObjectsAtDepth(depth);

            // Pre-allocate task list without LINQ Select
            var tasks = new List<Task>();
            foreach (var obj in objectsAtDepth)
            {
                tasks.Add(initializer(obj, cancellationToken).AsTask());
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }
    }

    #endregion

    #region Phase 4: Cleanup

    /// <summary>
    /// Cleans up after test execution.
    /// Decrements reference counts and disposes objects when count reaches zero.
    /// </summary>
    public Task CleanupTestAsync(TestContext testContext, List<Exception> cleanupExceptions)
    {
        return _objectTracker.UntrackObjects(testContext, cleanupExceptions).AsTask();
    }

    #endregion

    /// <summary>
    /// Clears the initialization cache. Called at end of test session.
    /// </summary>
    public void ClearCache()
    {
        _initializationTasks.Clear();
#if NET
        _spannedObjects.Clear();
#endif
    }
}
