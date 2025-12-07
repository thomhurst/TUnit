using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// Thread-safe service for initializing objects that implement <see cref="IAsyncInitializer"/>.
/// Provides deduplicated initialization with explicit phase control.
/// </summary>
/// <remarks>
/// <para>
/// This service delegates to the static <see cref="ObjectInitializer"/> to ensure consistent
/// behavior and avoid duplicate caches. This consolidates initialization tracking in one place.
/// </para>
/// </remarks>
internal sealed class ObjectInitializationService : IObjectInitializationService
{
    /// <summary>
    /// Creates a new instance of the initialization service.
    /// </summary>
    public ObjectInitializationService()
    {
        // No local cache needed - delegates to static ObjectInitializer
    }

    /// <inheritdoc />
    public ValueTask InitializeForDiscoveryAsync(object? obj, CancellationToken cancellationToken = default)
        => ObjectInitializer.InitializeForDiscoveryAsync(obj, cancellationToken);

    /// <inheritdoc />
    public ValueTask InitializeAsync(object? obj, CancellationToken cancellationToken = default)
        => ObjectInitializer.InitializeAsync(obj, cancellationToken);

    /// <inheritdoc />
    public bool IsInitialized(object? obj)
        => ObjectInitializer.IsInitialized(obj);

    /// <inheritdoc />
    public void ClearCache()
        => ObjectInitializer.ClearCache();
}
