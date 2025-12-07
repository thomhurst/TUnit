using System.Collections.Concurrent;
using TUnit.Core.Discovery;
using TUnit.Core.Interfaces;
using TUnit.Core.StaticProperties;

namespace TUnit.Core.Tracking;

/// <summary>
/// Provides trackable objects from test contexts for lifecycle management.
/// Delegates to <see cref="ObjectGraphDiscoverer"/> for the actual discovery logic.
/// </summary>
internal class TrackableObjectGraphProvider
{
    private readonly IObjectGraphDiscoverer _discoverer;

    /// <summary>
    /// Creates a new instance with the default discoverer.
    /// </summary>
    public TrackableObjectGraphProvider() : this(new ObjectGraphDiscoverer())
    {
    }

    /// <summary>
    /// Creates a new instance with a custom discoverer (for testing).
    /// </summary>
    public TrackableObjectGraphProvider(IObjectGraphDiscoverer discoverer)
    {
        _discoverer = discoverer;
    }

    /// <summary>
    /// Gets trackable objects from a test context, organized by depth level.
    /// Delegates to the shared ObjectGraphDiscoverer to eliminate code duplication.
    /// </summary>
    /// <param name="testContext">The test context to get trackable objects from.</param>
    /// <param name="cancellationToken">Optional cancellation token for long-running discovery.</param>
    public ConcurrentDictionary<int, HashSet<object>> GetTrackableObjects(TestContext testContext, CancellationToken cancellationToken = default)
    {
        // Use the ObjectGraphDiscoverer's specialized method that populates TrackedObjects directly
        if (_discoverer is ObjectGraphDiscoverer concreteDiscoverer)
        {
            return concreteDiscoverer.DiscoverAndTrackObjects(testContext, cancellationToken);
        }

        // Fallback for custom implementations (testing)
        var graph = _discoverer.DiscoverObjectGraph(testContext, cancellationToken);
        var trackedObjects = testContext.TrackedObjects;

        foreach (var (depth, objects) in graph.ObjectsByDepth)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var depthSet = trackedObjects.GetOrAdd(depth, _ => []);
            // Lock to ensure thread-safe HashSet modification
            lock (depthSet)
            {
                foreach (var obj in objects)
                {
                    depthSet.Add(obj);
                }
            }
        }

        return trackedObjects;
    }

    /// <summary>
    /// Gets trackable objects for static properties (session-level).
    /// </summary>
    public IEnumerable<object> GetStaticPropertyTrackableObjects()
    {
        foreach (var value in StaticPropertyRegistry.GetAllInitializedValues())
        {
            if (value != null)
            {
                yield return value;
            }
        }
    }
}
