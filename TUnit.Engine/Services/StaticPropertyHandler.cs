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
    private bool _initialized;

    public StaticPropertyHandler(TUnitFrameworkLogger logger,
        ObjectTracker objectTracker,
        TrackableObjectGraphProvider trackableObjectGraphProvider,
        Disposer disposer)
    {
        _logger = logger;
        _objectTracker = objectTracker;
        _trackableObjectGraphProvider = trackableObjectGraphProvider;
        _disposer = disposer;
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
}
