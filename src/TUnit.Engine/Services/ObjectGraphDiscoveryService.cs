using TUnit.Core;
using TUnit.Core.Discovery;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

/// <summary>
/// Service for discovering and organizing object graphs in TUnit.Engine.
/// Delegates to <see cref="ObjectGraphDiscoverer"/> in TUnit.Core for the actual discovery logic.
/// </summary>
internal sealed class ObjectGraphDiscoveryService
{
    private readonly IObjectGraphDiscoverer _discoverer;

    /// <summary>
    /// Creates a new instance with the default discoverer.
    /// </summary>
    public ObjectGraphDiscoveryService() : this(new ObjectGraphDiscoverer())
    {
    }

    /// <summary>
    /// Creates a new instance with a custom discoverer (for testing).
    /// </summary>
    public ObjectGraphDiscoveryService(IObjectGraphDiscoverer discoverer)
    {
        _discoverer = discoverer;
    }

    /// <summary>
    /// Discovers all objects from test context arguments and properties, organized by depth level.
    /// </summary>
    public ObjectGraph DiscoverObjectGraph(TestContext testContext, CancellationToken cancellationToken = default)
    {
        return _discoverer.DiscoverObjectGraph(testContext, cancellationToken);
    }

    /// <summary>
    /// Discovers nested objects from a single root object, organized by depth.
    /// </summary>
    public ObjectGraph DiscoverNestedObjectGraph(object rootObject, CancellationToken cancellationToken = default)
    {
        return _discoverer.DiscoverNestedObjectGraph(rootObject, cancellationToken);
    }
}
