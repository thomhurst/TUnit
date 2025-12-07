using System.Collections.Concurrent;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a contract for discovering object graphs from test contexts.
/// Pure query interface - only reads and returns data, does not modify state.
/// </summary>
/// <remarks>
/// <para>
/// Object graph discovery is used to find all objects that need initialization or disposal,
/// organized by their nesting depth in the object hierarchy.
/// </para>
/// <para>
/// The discoverer traverses:
/// <list type="bullet">
/// <item><description>Test class constructor arguments</description></item>
/// <item><description>Test method arguments</description></item>
/// <item><description>Injected property values</description></item>
/// <item><description>Nested objects that implement <see cref="IAsyncInitializer"/></description></item>
/// </list>
/// </para>
/// <para>
/// For tracking operations that modify TestContext.TrackedObjects, see <see cref="IObjectGraphTracker"/>.
/// </para>
/// </remarks>
internal interface IObjectGraphDiscoverer
{
    /// <summary>
    /// Discovers all objects from a test context, organized by depth level.
    /// </summary>
    /// <param name="testContext">The test context to discover objects from.</param>
    /// <param name="cancellationToken">Optional cancellation token for long-running discovery.</param>
    /// <returns>
    /// An <see cref="IObjectGraph"/> containing all discovered objects organized by depth.
    /// Depth 0 contains root objects (arguments and property values).
    /// Higher depths contain nested objects.
    /// </returns>
    IObjectGraph DiscoverObjectGraph(TestContext testContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers nested objects from a single root object, organized by depth.
    /// </summary>
    /// <param name="rootObject">The root object to discover nested objects from.</param>
    /// <param name="cancellationToken">Optional cancellation token for long-running discovery.</param>
    /// <returns>
    /// An <see cref="IObjectGraph"/> containing all discovered objects organized by depth.
    /// Depth 0 contains the root object itself.
    /// Higher depths contain nested objects.
    /// </returns>
    IObjectGraph DiscoverNestedObjectGraph(object rootObject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers objects and populates the test context's tracked objects dictionary directly.
    /// Used for efficient object tracking without intermediate allocations.
    /// </summary>
    /// <param name="testContext">The test context to discover objects from and populate.</param>
    /// <param name="cancellationToken">Optional cancellation token for long-running discovery.</param>
    /// <returns>
    /// The tracked objects dictionary (same as testContext.TrackedObjects) populated with discovered objects.
    /// </returns>
    /// <remarks>
    /// This method modifies testContext.TrackedObjects directly. For pure query operations,
    /// use <see cref="DiscoverObjectGraph"/> instead.
    /// </remarks>
    ConcurrentDictionary<int, HashSet<object>> DiscoverAndTrackObjects(TestContext testContext, CancellationToken cancellationToken = default);
}

/// <summary>
/// Marker interface for object graph tracking operations.
/// Extends <see cref="IObjectGraphDiscoverer"/> with operations that modify state.
/// </summary>
/// <remarks>
/// <para>
/// This interface exists to support Interface Segregation Principle:
/// clients that only need query operations can depend on <see cref="IObjectGraphDiscoverer"/>,
/// while clients that need tracking can depend on <see cref="IObjectGraphTracker"/>.
/// </para>
/// <para>
/// Currently inherits all methods from <see cref="IObjectGraphDiscoverer"/>.
/// The distinction exists for semantic clarity and future extensibility.
/// </para>
/// </remarks>
internal interface IObjectGraphTracker : IObjectGraphDiscoverer
{
    // All methods inherited from IObjectGraphDiscoverer
    // This interface provides semantic clarity for tracking operations
}

/// <summary>
/// Represents a discovered object graph organized by depth level.
/// </summary>
/// <remarks>
/// Collections are exposed as read-only to prevent callers from corrupting internal state.
/// Use <see cref="GetObjectsAtDepth"/> and <see cref="GetDepthsDescending"/> for safe iteration.
/// </remarks>
internal interface IObjectGraph
{
    /// <summary>
    /// Gets objects organized by depth (0 = root arguments, 1+ = nested).
    /// </summary>
    /// <remarks>
    /// Returns a read-only view. Use <see cref="GetObjectsAtDepth"/> for iteration.
    /// </remarks>
    IReadOnlyDictionary<int, IReadOnlyCollection<object>> ObjectsByDepth { get; }

    /// <summary>
    /// Gets all unique objects in the graph.
    /// </summary>
    /// <remarks>
    /// Returns a read-only view to prevent modification.
    /// </remarks>
    IReadOnlyCollection<object> AllObjects { get; }

    /// <summary>
    /// Gets the maximum nesting depth (-1 if empty).
    /// </summary>
    int MaxDepth { get; }

    /// <summary>
    /// Gets objects at a specific depth level.
    /// </summary>
    /// <param name="depth">The depth level to retrieve objects from.</param>
    /// <returns>An enumerable of objects at the specified depth, or empty if none exist.</returns>
    IEnumerable<object> GetObjectsAtDepth(int depth);

    /// <summary>
    /// Gets depth levels in descending order (deepest first).
    /// </summary>
    /// <returns>An enumerable of depth levels ordered from deepest to shallowest.</returns>
    IEnumerable<int> GetDepthsDescending();
}
