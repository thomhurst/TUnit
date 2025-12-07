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
public sealed class ObjectGraph : IObjectGraph
{
    private readonly ConcurrentDictionary<int, HashSet<object>> _objectsByDepth;
    private readonly HashSet<object> _allObjects;

    // Cached read-only views (created lazily on first access)
    private IReadOnlyDictionary<int, IReadOnlyCollection<object>>? _readOnlyObjectsByDepth;
    private IReadOnlyCollection<object>? _readOnlyAllObjects;

    /// <summary>
    /// Creates a new object graph from the discovered objects.
    /// </summary>
    /// <param name="objectsByDepth">Objects organized by depth level.</param>
    /// <param name="allObjects">All unique objects in the graph.</param>
    public ObjectGraph(ConcurrentDictionary<int, HashSet<object>> objectsByDepth, HashSet<object> allObjects)
    {
        _objectsByDepth = objectsByDepth;
        _allObjects = allObjects;
        // Use IsEmpty for thread-safe check before accessing Keys
        MaxDepth = objectsByDepth.IsEmpty ? -1 : objectsByDepth.Keys.Max();
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyCollection<object>> ObjectsByDepth
    {
        get
        {
            // Create read-only view lazily and cache it
            // Note: This creates a snapshot - subsequent modifications to internal collections won't be reflected
            return _readOnlyObjectsByDepth ??= new ReadOnlyDictionary<int, IReadOnlyCollection<object>>(
                _objectsByDepth.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyCollection<object>)kvp.Value.ToArray()));
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<object> AllObjects
    {
        get
        {
            // Create read-only view lazily and cache it
            return _readOnlyAllObjects ??= _allObjects.ToArray();
        }
    }

    /// <inheritdoc />
    public int MaxDepth { get; }

    /// <inheritdoc />
    public IEnumerable<object> GetObjectsAtDepth(int depth)
    {
        return _objectsByDepth.TryGetValue(depth, out var objects) ? objects : [];
    }

    /// <inheritdoc />
    public IEnumerable<int> GetDepthsDescending()
    {
        return _objectsByDepth.Keys.OrderByDescending(d => d);
    }
}
