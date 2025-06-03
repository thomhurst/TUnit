using System.Collections.Concurrent;
using TUnit.Core.Helpers;

namespace TUnit.Core.Data;

/// <summary>
/// A unified container for managing scoped test data instances with usage counting.
/// </summary>
/// <typeparam name="TKey">The type of the scoping key (e.g., Type for class scope, Assembly for assembly scope).</typeparam>
internal class ScopedContainer<TKey> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, ConcurrentDictionary<Type, ScopedInstance>> _containers = new();

    /// <summary>
    /// Gets or creates an instance for the specified key and type.
    /// </summary>
    /// <param name="key">The scoping key.</param>
    /// <param name="type">The type of object to retrieve or create.</param>
    /// <param name="factory">The factory function to create the instance if it doesn't exist.</param>
    /// <returns>The scoped instance containing the object and its usage counter.</returns>
    public ScopedInstance GetOrCreate(TKey key, Type type, Func<object> factory)
    {
        var container = _containers.GetOrAdd(key, _ => new ConcurrentDictionary<Type, ScopedInstance>());
        return container.GetOrAdd(type, _ => new ScopedInstance(factory(), new Counter()));
    }

    /// <summary>
    /// Attempts to get an existing instance for the specified key and type.
    /// </summary>
    /// <param name="key">The scoping key.</param>
    /// <param name="type">The type of object to retrieve.</param>
    /// <param name="instance">The scoped instance if found.</param>
    /// <returns>True if the instance was found; otherwise, false.</returns>
    public bool TryGet(TKey key, Type type, out ScopedInstance? instance)
    {
        instance = null;
        return _containers.TryGetValue(key, out var container) &&
               container.TryGetValue(type, out instance);
    }

    /// <summary>
    /// Removes an instance from the container.
    /// </summary>
    /// <param name="key">The scoping key.</param>
    /// <param name="type">The type of object to remove.</param>
    /// <returns>The removed instance, or null if not found.</returns>
    public ScopedInstance? Remove(TKey key, Type type)
    {
        if (!_containers.TryGetValue(key, out var container))
        {
            return null;
        }

        container.TryRemove(type, out var instance);

        // Clean up empty containers
        if (container.IsEmpty)
        {
            _containers.TryRemove(key, out _);
        }

        return instance;
    }

    /// <summary>
    /// Gets diagnostic information about the container state.
    /// </summary>
    /// <returns>A dictionary containing diagnostic information.</returns>
    public Dictionary<string, object> GetDiagnostics()
    {
        var scopeCount = _containers.Count;
        var totalInstances = _containers.Values.Sum(c => c.Count);

        return new Dictionary<string, object>
        {
            ["ScopeCount"] = scopeCount,
            ["TotalInstances"] = totalInstances,
            ["Scopes"] = _containers.Keys.Select(k => k.ToString()).ToList()
        };
    }
}
