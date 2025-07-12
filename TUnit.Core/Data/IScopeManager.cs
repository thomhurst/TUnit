namespace TUnit.Core.Data;

/// <summary>
/// Interface for managing object disposal in a specific scope.
/// </summary>
internal interface IScopeManager
{
    /// <summary>
    /// Gets or creates an instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to get or create.</typeparam>
    /// <param name="factory">The factory function to create the instance if it doesn't exist.</param>
    /// <returns>The instance.</returns>
    T GetOrCreate<T>(Func<T> factory);

    /// <summary>
    /// Increments the usage count for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to increment usage for.</typeparam>
    void IncrementUsage<T>();

    /// <summary>
    /// Attempts to dispose an instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to dispose.</typeparam>
    /// <param name="item">The item to dispose.</param>
    /// <returns>True if the item was disposed; false if it's still in use.</returns>
    Task<bool> TryDisposeAsync<T>(T item);
}
