using System.Reflection;

namespace TUnit.Core.Data;

/// <summary>
/// Manages class-scoped test data instances.
/// </summary>
internal class ClassScopeManager
{
    private readonly ScopedContainer<Type> _container = new();

    /// <summary>
    /// Gets or creates a class-scoped instance for a specific test class.
    /// </summary>
    /// <typeparam name="T">The type of object to get or create.</typeparam>
    /// <param name="testClassType">The test class type that defines the scope.</param>
    /// <param name="factory">The factory function to create the instance.</param>
    /// <returns>The instance.</returns>
    public T GetOrCreate<T>(Type testClassType, Func<T> factory)
    {
        var scopedInstance = _container.GetOrCreate(testClassType, typeof(T), () => factory()!);
        return (T)scopedInstance.Instance;
    }

    /// <summary>
    /// Increments the usage count for a class-scoped type in a specific test class.
    /// </summary>
    /// <typeparam name="T">The type to increment usage for.</typeparam>
    /// <param name="testClassType">The test class type that defines the scope.</param>
    public void IncrementUsage<T>(Type testClassType)
    {
        if (_container.TryGet(testClassType, typeof(T), out var instance) && instance != null)
        {
            instance.UsageCount.Increment();
        }
    }

    /// <summary>
    /// Attempts to dispose a class-scoped instance for a specific test class.
    /// </summary>
    /// <typeparam name="T">The type of object to dispose.</typeparam>
    /// <param name="testClassType">The test class type that defines the scope.</param>
    /// <param name="item">The item to dispose.</param>
    /// <returns>True if the item was disposed; false if it's still in use.</returns>
    public async Task<bool> TryDisposeAsync<T>(Type testClassType, T item)
    {
        if (!_container.TryGet(testClassType, typeof(T), out var instance) || instance == null)
        {
            // Object not tracked - dispose it directly
            await DisposeHelper.DisposeAsync(item);
            return true;
        }

        if (instance.UsageCount.Decrement() > 0)
        {
            return false; // Still in use
        }

        // Remove from container and dispose
        _container.Remove(testClassType, typeof(T));
        await DisposeHelper.DisposeAsync(item);
        return true;
    }

    /// <summary>
    /// Gets diagnostic information about the class scope.
    /// </summary>
    /// <returns>Diagnostic information.</returns>
    public Dictionary<string, object> GetDiagnostics()
    {
        return _container.GetDiagnostics();
    }
}
