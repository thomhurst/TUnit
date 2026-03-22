using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Data;

/// <summary>
/// Provides a thread-safe dictionary with lazy value initialization.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary. Must be non-null.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <remarks>
/// This class provides a concurrent dictionary where values are lazily initialized on first access.
/// Each value is created exactly once per key, even when accessed concurrently from multiple threads.
/// </remarks>
#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class ThreadSafeDictionary<TKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _innerDictionary = new();

    /// <summary>
    /// Gets a collection containing the keys in the dictionary.
    /// </summary>
    public ICollection<TKey> Keys => _innerDictionary.Keys;

    /// <summary>
    /// Gets an enumerable collection of values in the dictionary.
    /// </summary>
    public IEnumerable<TValue> Values => _innerDictionary.Values.Select(static lazy => lazy.Value);

    /// <summary>
    /// Gets the value associated with the specified key, or creates it if it doesn't exist.
    /// The factory is guaranteed to run at most once per key even under concurrent access.
    /// </summary>
    /// <remarks>
    /// Uses a fast-path TryGetValue to avoid allocating a Lazy on repeated calls,
    /// then falls back to GetOrAdd with a pre-created Lazy to prevent the factory
    /// running multiple times during a race.
    /// </remarks>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
    {
        if (_innerDictionary.TryGetValue(key, out var existingLazy))
        {
            return existingLazy.Value;
        }

        var newLazy = new Lazy<TValue>(() => func(key), LazyThreadSafetyMode.ExecutionAndPublication);
        var winning = _innerDictionary.GetOrAdd(key, newLazy);
        return winning.Value;
    }

    /// <summary>
    /// Gets the value associated with the specified key, or creates it using the factory and arg if it doesn't exist.
    /// Avoids closure allocation at the call site by accepting the factory argument explicitly.
    /// </summary>
    public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> func, TArg arg)
    {
        if (_innerDictionary.TryGetValue(key, out var existingLazy))
        {
            return existingLazy.Value;
        }

        var newLazy = new Lazy<TValue>(() => func(key, arg), LazyThreadSafetyMode.ExecutionAndPublication);
        var winning = _innerDictionary.GetOrAdd(key, newLazy);
        return winning.Value;
    }

    /// <summary>
    /// Tries to get the value associated with the specified key.
    /// </summary>
    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        if (_innerDictionary.TryGetValue(key, out var lazy))
        {
            value = lazy.Value!;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Removes the value with the specified key from the dictionary.
    /// Returns the value only if it had already been initialized; otherwise returns default.
    /// </summary>
    public TValue? Remove(TKey key)
    {
        if (_innerDictionary.TryRemove(key, out var lazy) && lazy.IsValueCreated)
        {
            return lazy.Value;
        }

        return default;
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the specified key is not found.</exception>
    public TValue this[TKey key] => _innerDictionary.TryGetValue(key, out var lazy)
        ? lazy.Value
        : throw new KeyNotFoundException($"Key '{key}' not found in dictionary");
}
