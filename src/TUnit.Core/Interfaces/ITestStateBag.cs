using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides a type-safe, thread-safe bag for storing and retrieving custom state during a test's execution.
/// Accessed via <see cref="TestContext.StateBag"/>.
/// </summary>
public interface ITestStateBag
{
    /// <summary>
    /// Gets the underlying concurrent dictionary for direct access.
    /// </summary>
    ConcurrentDictionary<string, object?> Items { get; }

    /// <summary>
    /// Gets or sets a value in the state bag.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>The value associated with the specified key.</returns>
    object? this[string key] { get; set; }

    /// <summary>
    /// Gets the number of items in the state bag.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets a value indicating whether the specified key exists in the state bag.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
    bool ContainsKey(string key);

    /// <summary>
    /// Gets the value associated with the specified key, or adds it if it does not exist.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key of the value to get or add.</param>
    /// <param name="valueFactory">The function used to generate a value for the key if it does not exist.</param>
    /// <returns>The value for the key. This will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
    /// <exception cref="InvalidCastException">Thrown if a value already exists for the key but is not of type <typeparamref name="T"/>.</exception>
    T GetOrAdd<T>(string key, Func<string, T> valueFactory);

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found and the value is of the correct type; otherwise, the default value for the type of the value parameter.</param>
    /// <returns><c>true</c> if the key was found and the value is of the correct type; otherwise, <c>false</c>.</returns>
    bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value);

    /// <summary>
    /// Attempts to remove a value with the specified key.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <param name="value">When this method returns, contains the object removed from the bag, or <c>null</c> if the key does not exist.</param>
    /// <returns><c>true</c> if the object was removed successfully; otherwise, <c>false</c>.</returns>
    bool TryRemove(string key, [MaybeNullWhen(false)] out object? value);
}
