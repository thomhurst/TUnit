using System.Collections.Concurrent;

namespace TUnit.Core.Lifecycle;

/// <summary>
/// Provides a unified mechanism for walking the object graph to discover
/// nested objects that need initialization or disposal tracking.
///
/// This interface consolidates the duplicate graph traversal logic from:
/// - DataSourceInitializer.CollectNestedObjects()
/// - PropertyInjectionService.RecurseIntoNestedPropertiesAsync()
/// - TrackableObjectGraphProvider.AddNestedTrackableObjects()
/// </summary>
public interface IObjectGraphWalker
{
    /// <summary>
    /// Walks the object graph starting from the root object and collects
    /// all objects that have injectable properties, organized by depth level.
    ///
    /// Depth 0 = root object
    /// Depth 1 = direct property values of root
    /// Depth N = property values at N levels deep
    /// </summary>
    /// <param name="root">The root object to start traversal from.</param>
    /// <param name="filter">Optional filter to exclude certain objects. Return true to include.</param>
    /// <returns>Dictionary mapping depth level to collection of objects at that level.</returns>
    IReadOnlyDictionary<int, IReadOnlyCollection<object>> WalkGraph(
        object root,
        Func<object, bool>? filter = null);

    /// <summary>
    /// Walks the object graph asynchronously, invoking a visitor for each object found.
    /// Objects are visited in depth-first order (deepest first) for proper initialization ordering.
    /// </summary>
    /// <param name="root">The root object to start traversal from.</param>
    /// <param name="visitor">Async callback invoked for each object with (object, depth).</param>
    /// <param name="filter">Optional filter to exclude certain objects. Return true to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask WalkGraphAsync(
        object root,
        Func<object, int, ValueTask> visitor,
        Func<object, bool>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Collects all objects in the graph into a pre-existing dictionary, organized by depth.
    /// This is useful when building up a collection across multiple root objects.
    /// </summary>
    /// <param name="root">The root object to start traversal from.</param>
    /// <param name="objectsByDepth">Dictionary to populate with discovered objects.</param>
    /// <param name="visitedObjects">Set of already-visited objects to prevent cycles and duplicates.</param>
    /// <param name="startDepth">Starting depth level for this traversal.</param>
    void CollectObjects(
        object root,
        ConcurrentDictionary<int, HashSet<object>> objectsByDepth,
        HashSet<object> visitedObjects,
        int startDepth = 0);
}
