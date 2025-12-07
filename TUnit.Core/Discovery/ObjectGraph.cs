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
/// Uses Lazy&lt;T&gt; for thread-safe lazy initialization of read-only views.
/// </remarks>
public sealed class ObjectGraph : IObjectGraph
{
    private readonly ConcurrentDictionary<int, HashSet<object>> _objectsByDepth;
    private readonly HashSet<object> _allObjects;

    // Thread-safe lazy initialization of read-only views
    private readonly Lazy<IReadOnlyDictionary<int, IReadOnlyCollection<object>>> _lazyReadOnlyObjectsByDepth;
    private readonly Lazy<IReadOnlyCollection<object>> _lazyReadOnlyAllObjects;

    // Cached sorted depths (computed once in constructor)
    private readonly int[] _sortedDepthsDescending;

    /// <summary>
    /// Creates a new object graph from the discovered objects.
    /// </summary>
    /// <param name="objectsByDepth">Objects organized by depth level.</param>
    /// <param name="allObjects">All unique objects in the graph.</param>
    public ObjectGraph(ConcurrentDictionary<int, HashSet<object>> objectsByDepth, HashSet<object> allObjects)
    {
        _objectsByDepth = objectsByDepth;
        _allObjects = allObjects;

        // Compute MaxDepth and sorted depths without LINQ to reduce allocations
        var keyCount = objectsByDepth.Count;
        if (keyCount == 0)
        {
            MaxDepth = -1;
            _sortedDepthsDescending = [];
        }
        else
        {
            var keys = new int[keyCount];
            objectsByDepth.Keys.CopyTo(keys, 0);

            // Find max manually
            var maxDepth = int.MinValue;
            foreach (var key in keys)
            {
                if (key > maxDepth)
                {
                    maxDepth = key;
                }
            }
            MaxDepth = maxDepth;

            // Sort in descending order using Array.Sort with reverse comparison
            Array.Sort(keys, (a, b) => b.CompareTo(a));
            _sortedDepthsDescending = keys;
        }

        // Use Lazy<T> with ExecutionAndPublication for thread-safe single initialization
        _lazyReadOnlyObjectsByDepth = new Lazy<IReadOnlyDictionary<int, IReadOnlyCollection<object>>>(
            CreateReadOnlyObjectsByDepth,
            LazyThreadSafetyMode.ExecutionAndPublication);

        _lazyReadOnlyAllObjects = new Lazy<IReadOnlyCollection<object>>(
            () => _allObjects.ToArray(),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyCollection<object>> ObjectsByDepth => _lazyReadOnlyObjectsByDepth.Value;

    /// <inheritdoc />
    public IReadOnlyCollection<object> AllObjects => _lazyReadOnlyAllObjects.Value;

    /// <inheritdoc />
    public int MaxDepth { get; }

    /// <inheritdoc />
    public IEnumerable<object> GetObjectsAtDepth(int depth)
    {
        if (!_objectsByDepth.TryGetValue(depth, out var objects))
        {
            return [];
        }

        // Lock and copy to prevent concurrent modification issues
        lock (objects)
        {
            return objects.ToArray();
        }
    }

    /// <inheritdoc />
    public IEnumerable<int> GetDepthsDescending()
    {
        // Return cached sorted depths (computed once in constructor)
        return _sortedDepthsDescending;
    }

    /// <summary>
    /// Creates a thread-safe read-only snapshot of objects by depth.
    /// </summary>
    private IReadOnlyDictionary<int, IReadOnlyCollection<object>> CreateReadOnlyObjectsByDepth()
    {
        var dict = new Dictionary<int, IReadOnlyCollection<object>>(_objectsByDepth.Count);

        foreach (var kvp in _objectsByDepth)
        {
            // Lock each HashSet while copying to ensure consistency
            lock (kvp.Value)
            {
                dict[kvp.Key] = kvp.Value.ToArray();
            }
        }

        return new ReadOnlyDictionary<int, IReadOnlyCollection<object>>(dict);
    }
}
