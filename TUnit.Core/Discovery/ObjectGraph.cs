using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Discovery;

/// <summary>
/// Represents a discovered object graph organized by depth level.
/// </summary>
/// <remarks>
/// Internal collections are stored privately and exposed as read-only views
/// to prevent callers from corrupting internal state.
/// </remarks>
internal readonly struct ObjectGraph
{
    private readonly ConcurrentDictionary<int, HashSet<object>> _objectsByDepth;

    // Cached sorted depths (computed once in constructor)
    private readonly int[] _sortedDepthsDescending;

    /// <summary>
    /// Creates a new object graph from the discovered objects.
    /// </summary>
    /// <param name="objectsByDepth">Objects organized by depth level.</param>
    /// <param name="allObjects">All unique objects in the graph.</param>
    public ObjectGraph(ConcurrentDictionary<int, HashSet<object>> objectsByDepth)
    {
        _objectsByDepth = objectsByDepth;

        // Compute MaxDepth and sorted depths without LINQ to reduce allocations
        var keyCount = objectsByDepth.Count;
        if (keyCount == 0)
        {
            _sortedDepthsDescending = [];
        }
        else
        {
            var keys = objectsByDepth.Keys.ToArray();

            // Sort in descending order using Array.Sort with reverse comparison
            Array.Sort(keys, (a, b) => b.CompareTo(a));
            _sortedDepthsDescending = keys;
        }
    }

    /// <summary>
    /// Gets objects at a specific depth level.
    /// </summary>
    /// <param name="depth">The depth level to retrieve objects from.</param>
    /// <returns>An ReadOnlyCollection of objects at the specified depth, or empty if none exist.</returns>
    public IReadOnlyCollection<object> GetObjectsAtDepth(int depth)
    {
        if (!_objectsByDepth.TryGetValue(depth, out var objects))
        {
            return [];
        }

        return objects;
    }

    /// <summary>
    /// Gets depth levels in descending order (deepest first).
    /// </summary>
    /// <returns>An enumerable of depth levels ordered from deepest to shallowest.</returns>
    public IEnumerable<int> GetDepthsDescending()
    {
        // Return cached sorted depths (computed once in constructor)
        return _sortedDepthsDescending;
    }
}
