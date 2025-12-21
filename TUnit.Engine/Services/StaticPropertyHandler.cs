using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.StaticProperties;
using TUnit.Core.Tracking;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

/// <summary>
/// Service responsible for initializing static properties registered via source generation
/// </summary>
internal sealed class StaticPropertyHandler
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ObjectTracker _objectTracker;
    private readonly TrackableObjectGraphProvider _trackableObjectGraphProvider;
    private readonly Disposer _disposer;
    private readonly Lazy<PropertyInjector> _propertyInjector;
    private readonly ObjectGraphDiscoveryService _objectGraphDiscoveryService;
    private readonly ConcurrentDictionary<string, object?> _sessionObjectBag = new();
    private bool _initialized;

    public StaticPropertyHandler(TUnitFrameworkLogger logger,
        ObjectTracker objectTracker,
        TrackableObjectGraphProvider trackableObjectGraphProvider,
        Disposer disposer,
        Lazy<PropertyInjector> propertyInjector,
        ObjectGraphDiscoveryService objectGraphDiscoveryService)
    {
        _logger = logger;
        _objectTracker = objectTracker;
        _trackableObjectGraphProvider = trackableObjectGraphProvider;
        _disposer = disposer;
        _propertyInjector = propertyInjector;
        _objectGraphDiscoveryService = objectGraphDiscoveryService;
    }

    /// <summary>
    /// Initialize all registered static properties before tests run
    /// </summary>
    public async Task InitializeStaticPropertiesAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        var properties = StaticPropertyRegistry.GetRegisteredProperties();

        foreach (var property in properties)
        {
            try
            {
                var value = await property.InitializerAsync();

                if (value != null)
                {
                    // Inject instance properties on the value before initialization
                    // This handles cases where the static property's type has instance properties
                    // with data source attributes (e.g., [ClassDataSource] with Shared = PerTestSession)
                    await _propertyInjector.Value.InjectPropertiesAsync(
                        value,
                        _sessionObjectBag,
                        methodMetadata: null,
                        new TestContextEvents());

                    // Initialize nested objects depth-first BEFORE initializing the main value
                    // This ensures containers (IAsyncInitializer) are started before the factory
                    // that depends on them calls methods like GetConnectionString()
                    await InitializeNestedObjectsAsync(value, cancellationToken);

                    // Initialize the value (IAsyncInitializer, etc.)
                    await ObjectInitializer.InitializeAsync(value, cancellationToken);

                    // Store for tracking
                    StaticPropertyRegistry.StoreInitializedValue(property.DeclaringType, property.PropertyName, value);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize static property {property.DeclaringType.Name}.{property.PropertyName}", ex);
            }
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
    /// Dispose all tracked static properties at session end
    /// </summary>
    public async Task DisposeStaticPropertiesAsync(List<Exception> cleanupExceptions)
    {
        var staticProperties = _trackableObjectGraphProvider.GetStaticPropertyTrackableObjects();

        foreach (var staticProperty in staticProperties)
        {
            try
            {
                await _disposer.DisposeAsync(staticProperty).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                cleanupExceptions.Add(e);
            }
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
