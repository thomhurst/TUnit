using System.Collections.Concurrent;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Discovery;

/// <summary>
/// Represents a discovered object graph organized by depth level.
/// </summary>
public sealed class ObjectGraph : IObjectGraph
{
    /// <summary>
    /// Creates a new object graph from the discovered objects.
    /// </summary>
    /// <param name="objectsByDepth">Objects organized by depth level.</param>
    /// <param name="allObjects">All unique objects in the graph.</param>
    public ObjectGraph(ConcurrentDictionary<int, HashSet<object>> objectsByDepth, HashSet<object> allObjects)
    {
        ObjectsByDepth = objectsByDepth;
        AllObjects = allObjects;
        // Use IsEmpty for thread-safe check before accessing Keys
        MaxDepth = objectsByDepth.IsEmpty ? -1 : objectsByDepth.Keys.Max();
    }

    /// <inheritdoc />
    public ConcurrentDictionary<int, HashSet<object>> ObjectsByDepth { get; }

    /// <inheritdoc />
    public HashSet<object> AllObjects { get; }

    /// <inheritdoc />
    public int MaxDepth { get; }

    /// <inheritdoc />
    public IEnumerable<object> GetObjectsAtDepth(int depth)
    {
        return ObjectsByDepth.TryGetValue(depth, out var objects) ? objects : [];
    }

    /// <inheritdoc />
    public IEnumerable<int> GetDepthsDescending()
    {
        return ObjectsByDepth.Keys.OrderByDescending(d => d);
    }
}
