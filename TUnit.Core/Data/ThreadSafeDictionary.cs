using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Data;

/// <summary>
/// Provides a thread-safe dictionary with lazy value initialization.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary. Must be non-null.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary. Must have a public parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// This class provides a concurrent dictionary where values are lazily initialized on first access.
/// Each value is created exactly once per key, even when accessed concurrently from multiple threads.
/// </para>
/// <para>
/// The lazy initialization is thread-safe and uses <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>
/// to ensure that the factory function is executed only once per key, even under concurrent access.
/// </para>
/// <para>
/// This class is marked as hidden in the editor browsable state in non-debug builds as it's primarily
/// intended for internal use within the TUnit framework.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var cache = new ThreadSafeDictionary&lt;string, ExpensiveResource&gt;();
///
/// // Values are created lazily on first access
/// var resource1 = cache.GetOrAdd("key1", k => new ExpensiveResource(k));
///
/// // Same key returns the same instance (thread-safe)
/// var resource2 = cache.GetOrAdd("key1", k => new ExpensiveResource(k));
/// // resource1 == resource2
/// </code>
/// </example>
#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public class ThreadSafeDictionary<TKey,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _innerDictionary = new();

    /// <summary>
    /// Gets a collection containing the keys in the dictionary.
    /// </summary>
    /// <value>A collection of keys present in the dictionary.</value>
    public ICollection<TKey> Keys => _innerDictionary.Keys;

    /// <summary>
    /// Gets an enumerable collection of values in the dictionary.
    /// </summary>
    /// <value>An enumerable of initialized values.</value>
    /// <remarks>
    /// Accessing this property will force initialization of all lazy values in the dictionary.
    /// </remarks>
    public IEnumerable<TValue> Values => _innerDictionary.Values.Select(static lazy => lazy.Value);

    /// <summary>
    /// Gets the value associated with the specified key, or creates it if it doesn't exist.
    /// </summary>
    /// <param name="key">The key of the value to get or create.</param>
    /// <param name="func">The factory function to create the value if the key doesn't exist.</param>
    /// <returns>
    /// The value for the key. This will be either the existing value or a newly created value from the factory function.
    /// </returns>
    /// <remarks>
    /// This method is thread-safe. If multiple threads call this method simultaneously with the same key,
    /// the factory function will be executed only once, and all threads will receive the same instance.
    /// This implementation uses a two-phase approach: TryGetValue for the fast path (when key exists),
    /// and GetOrAdd with a pre-created Lazy for the slow path (new key). This prevents the factory
    /// from being invoked multiple times during concurrent access.
    /// </remarks>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
    {
        // Fast path: Check if key already exists (lock-free read)
        if (_innerDictionary.TryGetValue(key, out var existingLazy))
        {
            return existingLazy.Value;
        }

        return GetOrAddSlow(key, func);
    }

    private TValue GetOrAddSlow(TKey key, Func<TKey, TValue> func)
    {
        // Slow path: Key not found, need to create
        // Create Lazy instance OUTSIDE of GetOrAdd to prevent factory from running during race
        var newLazy = new Lazy<TValue>(() => func(key), LazyThreadSafetyMode.ExecutionAndPublication);

        // Use GetOrAdd with VALUE (not factory) - atomic operation that either:
        // 1. Adds our newLazy if key still doesn't exist
        // 2. Returns existing Lazy if another thread just added one
        var winningLazy = _innerDictionary.GetOrAdd(key, newLazy);

        // CRITICAL: Always return value from the Lazy that's actually in the dictionary
        // This ensures only ONE factory execution even if multiple Lazy instances were created
        return winningLazy.Value;
    }

    /// <summary>
    /// Tries to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified key if found;
    /// otherwise, the default value for the type.
    /// </param>
    /// <returns><see langword="true"/> if the dictionary contains an element with the specified key; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// If the key exists, accessing the value will trigger lazy initialization if it hasn't occurred yet.
    /// </remarks>
    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        if (_innerDictionary.TryGetValue(key, out var lazy))
        {
            value = lazy.Value!;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Removes the value with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key of the value to remove.</param>
    /// <returns>
    /// The value that was removed, or the default value for the type if the key was not found.
    /// </returns>
    /// <remarks>
    /// If the key exists and the value has been lazily initialized, that instance is returned.
    /// Otherwise, the default value for <typeparamref name="TValue"/> is returned.
    /// </remarks>
    public TValue? Remove(TKey key)
    {
        if (_innerDictionary.TryRemove(key, out var lazy))
        {
            return lazy.Value;
        }

        return default(TValue?);
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <returns>The value associated with the specified key.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the specified key is not found in the dictionary.</exception>
    /// <remarks>
    /// Accessing this indexer will trigger lazy initialization of the value if it hasn't occurred yet.
    /// </remarks>
    public TValue this[TKey key] => _innerDictionary.TryGetValue(key, out var lazy)
        ? lazy.Value
        : throw new KeyNotFoundException($"Key '{key}' not found in dictionary");
}
