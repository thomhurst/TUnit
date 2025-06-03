using TUnit.Core.Helpers;

namespace TUnit.Core.Data;

/// <summary>
/// Manages globally scoped test data instances.
/// </summary>
internal class GlobalScopeManager : IScopeManager
{
    private readonly ScopedContainer<string> _container = new();
    private const string GlobalKey = "global";

    /// <summary>
    /// Gets or creates a globally scoped instance.
    /// </summary>
    /// <typeparam name="T">The type of object to get or create.</typeparam>
    /// <param name="factory">The factory function to create the instance.</param>
    /// <returns>The instance.</returns>
    public T GetOrCreate<T>(Func<T> factory)
    {
        var scopedInstance = _container.GetOrCreate(GlobalKey, typeof(T), () => factory()!);
        return (T)scopedInstance.Instance;
    }    /// <summary>
    /// Increments the usage count for a globally scoped type.
    /// </summary>
    /// <typeparam name="T">The type to increment usage for.</typeparam>
    public void IncrementUsage<T>()
    {
        if (_container.TryGet(GlobalKey, typeof(T), out var instance) && instance != null)
        {
            instance.UsageCount.Increment();
        }
    }    /// <summary>
    /// Attempts to dispose a globally scoped instance.
    /// </summary>
    /// <typeparam name="T">The type of object to dispose.</typeparam>
    /// <param name="item">The item to dispose.</param>
    /// <returns>True if the item was disposed; false if it's still in use.</returns>
    public async Task<bool> TryDisposeAsync<T>(T item)
    {
        if (!_container.TryGet(GlobalKey, typeof(T), out var instance) || instance == null)
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
        _container.Remove(GlobalKey, typeof(T));
        await DisposeHelper.DisposeAsync(item);
        return true;
    }

    /// <summary>
    /// Gets diagnostic information about the global scope.
    /// </summary>
    /// <returns>Diagnostic information.</returns>
    public Dictionary<string, object> GetDiagnostics()
    {
        return _container.GetDiagnostics();
    }
}

/// <summary>
/// Helper class for disposing objects.
/// </summary>
internal static class DisposeHelper
{
    /// <summary>
    /// Disposes an object asynchronously if it implements IAsyncDisposable or IDisposable.
    /// </summary>
    /// <param name="obj">The object to dispose.</param>
    /// <returns>A task representing the disposal operation.</returns>
    public static async ValueTask DisposeAsync(object? obj)
    {
        switch (obj)
        {
            case null:
                return;
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }
}
