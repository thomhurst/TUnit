using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Discovery;
using TUnit.Core.Helpers;
using TUnit.Core.StaticProperties;
using TUnit.Core.Tracking;
using TUnit.Engine.Logging;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.Engine.Services;

/// <summary>
/// Service responsible for initializing static properties registered via source generation
/// </summary>
internal sealed class StaticPropertyHandler
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ObjectTracker _objectTracker;
    private readonly TrackableObjectGraphProvider _trackableObjectGraphProvider;
    private readonly Lazy<PropertyInjector> _propertyInjector;
    private readonly ObjectGraphDiscoveryService _objectGraphDiscoveryService;
    private readonly ConcurrentDictionary<string, object?> _sessionObjectBag = new();
    private readonly TestBuilderContext _staticPropertyContext;
    private bool _initialized;
    private Exception? _initializationError;
    private static readonly ConditionalWeakTable<StaticPropertyMetadata, SharedInitialization> s_sharedInitializations = new();

    private sealed class SharedInitialization
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private int _owners;
        private TestBuilderContext? _context;
        private readonly List<object> _trackedObjects = new();
        private Exception? _error;
        private ObjectTracker? _tracker;
        private StaticPropertyMetadata? _property;

        public async ValueTask AcquireAsync(StaticPropertyHandler handler, StaticPropertyMetadata property, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var first = _owners++ == 0;
                // Subscribe before initialization so provider disposal also releases partial failures.
                TestSessionContext.RegisterStaticOnDispose(handler._staticPropertyContext, (_, _) => ReleaseAsync());
                if (!first)
                {
                    if (_error is { } error)
                    {
                        throw new InvalidOperationException("Shared static property initialization failed", error);
                    }
                    return;
                }

                _context = new TestBuilderContext { TestMetadata = handler._staticPropertyContext.TestMetadata };
                _tracker = handler._objectTracker;
                _property = property;
                var previous = TestSessionContext.StaticInitializationContext.Value;
                TestSessionContext.StaticInitializationContext.Value = _context;
                try
                {
                    var value = await property.InitializerAsync().ConfigureAwait(false);
                    if (value is not null)
                    {
                        _tracker.TrackStaticLeaseObject(value);
                        _trackedObjects.Add(value);
                        await handler._propertyInjector.Value.InjectPropertiesAsync(value, handler._sessionObjectBag,
                            methodMetadata: null, _context.Events).ConfigureAwait(false);
                        // Use execution's ownership-aware traversal: DI-owned children stay with
                        // their scope, while TUnit-injected fixtures retain independent ownership.
                        var graph = new ObjectGraphDiscoverer().DiscoverStaticPropertyTrackableObjects(value, cancellationToken);
                        foreach (var level in graph)
                        {
                            foreach (var obj in level.Value)
                            {
                                if (ReferenceEquals(obj, value)) continue;
                                _tracker.TrackStaticLeaseObject(obj);
                                _trackedObjects.Add(obj);
                            }
                        }
                        await handler.InitializeNestedObjectsAsync(value, cancellationToken).ConfigureAwait(false);
                        await ObjectInitializer.InitializeAsync(value, cancellationToken).ConfigureAwait(false);
                        StaticPropertyRegistry.StoreInitializedValue(property.DeclaringType, property.PropertyName, value);
                    }
                }
                catch (Exception ex)
                {
                    _error = ex;
                    throw;
                }
                finally
                {
                    TestSessionContext.StaticInitializationContext.Value = previous;
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        private async ValueTask ReleaseAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (--_owners != 0) return;
                var context = _context!;
                var tracker = _tracker!;
                StaticPropertyRegistry.RemoveInitializedValue(_property!.DeclaringType, _property.PropertyName);
                _property = null;
                _context = null;
                _error = null;
                _tracker = null;
                var errors = new List<Exception>();
                // A fixture (including a nested fixture) can also belong to another static
                // property. Keep this graph's scopes until every fixture's final lease ends.
                var remaining = _trackedObjects.Count;
                async ValueTask ReleaseScopesAsync()
                {
                    if (Interlocked.Decrement(ref remaining) != 0) return;
                    var scopeErrors = new List<Exception>();
                    await TestOnDisposeRaiser.RaiseForSessionAsync(context, scopeErrors).ConfigureAwait(false);
                    if (scopeErrors.Count > 0) throw new AggregateException("Errors occurred while disposing static scopes", scopeErrors);
                }
                foreach (var obj in _trackedObjects)
                {
                    try { await tracker.ReleaseStaticLeaseObjectAsync(obj, ReleaseScopesAsync).ConfigureAwait(false); }
                    catch (Exception ex) { errors.Add(ex); }
                }
                if (_trackedObjects.Count == 0)
                {
                    await TestOnDisposeRaiser.RaiseForSessionAsync(context, errors).ConfigureAwait(false);
                }
                _trackedObjects.Clear();
                if (errors.Count > 0) throw new AggregateException("Errors occurred while disposing shared static property", errors);
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    public StaticPropertyHandler(TUnitFrameworkLogger logger,
        ObjectTracker objectTracker,
        TrackableObjectGraphProvider trackableObjectGraphProvider,
        Lazy<PropertyInjector> propertyInjector,
        ObjectGraphDiscoveryService objectGraphDiscoveryService,
        TestSessionContext sessionContext)
    {
        _staticPropertyContext = sessionContext.StaticPropertyContext;
        _logger = logger;
        _objectTracker = objectTracker;
        _trackableObjectGraphProvider = trackableObjectGraphProvider;
        _propertyInjector = propertyInjector;
        _objectGraphDiscoveryService = objectGraphDiscoveryService;
    }

    /// <summary>
    /// Initialize all registered static properties before tests run
    /// </summary>
    public Task InitializeStaticPropertiesAsync(CancellationToken cancellationToken)
        => InitializeStaticPropertiesAsync(StaticPropertyRegistry.GetRegisteredProperties(), cancellationToken);

    internal async Task InitializeStaticPropertiesAsync(IEnumerable<StaticPropertyMetadata> properties, CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            if (_initializationError is { } error) throw new InvalidOperationException("Static property initialization previously failed", error);
            return;
        }
        _initialized = true;

        foreach (var property in properties)
        {
            try
            {
                await s_sharedInitializations.GetValue(property, static _ => new())
                    .AcquireAsync(this, property, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _initializationError = new InvalidOperationException($"Failed to initialize static property {property.DeclaringType.Name}.{property.PropertyName}", ex);
                throw _initializationError;
            }
        }
    }

    /// <summary>Release subscriptions when initialization fails before normal session cleanup.</summary>
    public async ValueTask ReleaseSessionCallbacksAfterFailedInitializationAsync()
    {
        var exceptions = new List<Exception>();
        await TestOnDisposeRaiser.RaiseForSessionAsync(_staticPropertyContext, exceptions).ConfigureAwait(false);
        foreach (var exception in exceptions)
        {
            await _logger.LogErrorAsync($"Error during static property cleanup after failed initialization: {exception}").ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Track all initialized static properties
    /// This should be called once all static properties are initialized and before tests begin
    /// </summary>
    public void TrackStaticProperties()
    {
        _objectTracker.TrackStaticProperties();
    }

    /// <summary>
    /// Dispose all tracked static properties at session end.
    /// Goes through the ObjectTracker (decrementing the +1 reference TrackStaticProperties took)
    /// rather than disposing directly, so the tracking entry is removed and the session-end
    /// sweep in TestSessionCoordinator does not dispose the same object a second time.
    /// </summary>
    public Task DisposeStaticPropertiesAsync(List<Exception> cleanupExceptions)
        => DisposeStaticPropertiesAsync(cleanupExceptions, _trackableObjectGraphProvider.GetStaticPropertyTrackableObjects());

    internal async Task DisposeStaticPropertiesAsync(List<Exception> cleanupExceptions, IEnumerable<object> staticProperties)
    {
        foreach (var staticProperty in staticProperties)
        {
            try
            {
                await _objectTracker.UntrackObjectAsync(staticProperty).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                cleanupExceptions.Add(e);
            }
        }

        // Fixtures may use injected services during Dispose/DisposeAsync. Release their scopes
        // only after every fixture has been untracked, even when a fixture disposal failed.
        await TestOnDisposeRaiser.RaiseForSessionAsync(_staticPropertyContext, cleanupExceptions).ConfigureAwait(false);

        if (cleanupExceptions.Count > 0)
        {
            throw new AggregateException("Errors occurred while disposing static properties", cleanupExceptions);
        }
    }

    internal static async Task DisposeStaticPropertiesAsync(
        IEnumerable<object> staticProperties,
        TestContextEvents events,
        ObjectTracker objectTracker,
        List<Exception> cleanupExceptions)
    {
        // Detach before awaiting so a reused host cannot retain scopes from earlier sessions.
        var scopes = events.DetachDataRowScopes();

        foreach (var staticProperty in staticProperties)
        {
            // Scope-owned values must be released by their scope before tracker disposal is skipped.
            if (scopes is not null)
            {
                await scopes.ReleaseAsync(staticProperty, cleanupExceptions).ConfigureAwait(false);
            }
            try
            {
                await objectTracker.UntrackObjectAsync(staticProperty).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                cleanupExceptions.Add(e);
            }
        }

        // Also release scopes whose factory failed or whose returned value was not trackable.
        if (scopes is not null)
        {
            await scopes.CompleteAsync(cleanupExceptions).ConfigureAwait(false);
        }

        if (cleanupExceptions.Count > 0)
        {
            throw new AggregateException("Errors occurred while disposing static properties", cleanupExceptions);
        }
    }

    /// <summary>
    /// Initializes nested objects depth-first (deepest first) before the parent object.
    /// This ensures that containers/resources are fully initialized before the parent
    /// object (like a WebApplicationFactory) tries to use them.
    /// </summary>
    private async Task InitializeNestedObjectsAsync(object rootObject, CancellationToken cancellationToken)
    {
        var graph = _objectGraphDiscoveryService.DiscoverNestedObjectGraph(rootObject, cancellationToken);

        // Initialize from deepest to shallowest (skip depth 0 which is the root itself)
        foreach (var depth in graph.GetDepthsDescending())
        {
            if (depth == 0)
            {
                continue; // Root handled separately by caller
            }

            var objectsAtDepth = graph.GetObjectsAtDepth(depth);

            // Pre-allocate task list without LINQ Select
            var tasks = new List<Task>();
            foreach (var obj in objectsAtDepth)
            {
                tasks.Add(ObjectInitializer.InitializeAsync(obj, cancellationToken).AsTask());
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }
    }
}
