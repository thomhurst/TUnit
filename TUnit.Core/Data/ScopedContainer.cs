namespace TUnit.Core.Data;

/// <summary>
/// A unified container for managing scoped test data instances.
/// </summary>
/// <typeparam name="TKey">The type of the scoping key (e.g., Type for class scope, Assembly for assembly scope).</typeparam>
internal class ScopedContainer<TKey> where TKey : notnull
{
    private readonly GetOnlyDictionary<TKey, GetOnlyDictionary<Type, object>> _containers = new();

    /// <summary>
    /// Gets or creates an instance for the specified key and type.
    /// </summary>
    /// <param name="key">The scoping key.</param>
    /// <param name="type">The type of object to retrieve or create.</param>
    /// <param name="factory">The factory function to create the instance if it doesn't exist.</param>
    /// <returns>The instance.</returns>
    public object GetOrCreate(TKey key, Type type, Func<object> factory)
    {
        var container = _containers.GetOrAdd(key, _ => new GetOnlyDictionary<Type, object>());
        return container.GetOrAdd(type, _ => factory());
    }

    /// <summary>
    /// Attempts to get an existing instance for the specified key and type.
    /// </summary>
    /// <param name="key">The scoping key.</param>
    /// <param name="type">The type of object to retrieve.</param>
    /// <param name="instance">The instance if found.</param>
    /// <returns>True if the instance was found; otherwise, false.</returns>
    public bool TryGet(TKey key, Type type, out object? instance)
    {
        instance = null;
        return _containers.TryGetValue(key, out var container) &&
               container.TryGetValue(type, out instance);
    }

}
