using System.Collections.Concurrent;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a contract for discovering object graphs from test contexts.
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
/// </remarks>
public interface IObjectGraphDiscoverer
{
    /// <summary>
    /// Discovers all objects from a test context, organized by depth level.
    /// </summary>
    /// <param name="testContext">The test context to discover objects from.</param>
    /// <returns>
    /// An <see cref="IObjectGraph"/> containing all discovered objects organized by depth.
    /// Depth 0 contains root objects (arguments and property values).
    /// Higher depths contain nested objects.
    /// </returns>
    IObjectGraph DiscoverObjectGraph(TestContext testContext);

    /// <summary>
    /// Discovers nested objects from a single root object, organized by depth.
    /// </summary>
    /// <param name="rootObject">The root object to discover nested objects from.</param>
    /// <returns>
    /// An <see cref="IObjectGraph"/> containing all discovered objects organized by depth.
    /// Depth 0 contains the root object itself.
    /// Higher depths contain nested objects.
    /// </returns>
    IObjectGraph DiscoverNestedObjectGraph(object rootObject);
}

/// <summary>
/// Represents a discovered object graph organized by depth level.
/// </summary>
public interface IObjectGraph
{
    /// <summary>
    /// Gets objects organized by depth (0 = root arguments, 1+ = nested).
    /// </summary>
    ConcurrentDictionary<int, HashSet<object>> ObjectsByDepth { get; }

    /// <summary>
    /// Gets all unique objects in the graph.
    /// </summary>
    HashSet<object> AllObjects { get; }

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
