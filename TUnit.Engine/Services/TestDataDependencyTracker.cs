using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Services;

/// <summary>
/// Tracks dependencies between test data objects at the framework level.
/// This ensures proper disposal order regardless of whether using source generation or reflection mode.
/// </summary>
internal class TestDataDependencyTracker : IDependencyTracker
{
    private readonly DependencyTracker _dependencyTracker = new();
    private readonly object _lock = new();

    /// <summary>
    /// Registers a dependency between a parent object and its child object.
    /// </summary>
    /// <param name="parent">The parent object that depends on the child.</param>
    /// <param name="child">The child object that the parent depends on.</param>
    /// <param name="childSharedType">The shared type of the child object.</param>
    /// <param name="childKey">The key of the child object (for keyed sharing).</param>
    public void RegisterDependency(object parent, object child, SharedType childSharedType, string? childKey = null)
    {
        lock (_lock)
        {
            // Create a scope manager based on the shared type
            var scopeManager = CreateScopeManager(childSharedType, childKey);
            _dependencyTracker.RegisterDependency(parent, child, scopeManager);
        }
    }

    /// <summary>
    /// Disposes an object and all its nested dependencies in the correct order.
    /// </summary>
    /// <param name="item">The item to dispose along with its dependencies.</param>
    public async Task DisposeWithDependenciesAsync(object item)
    {
        await _dependencyTracker.DisposeNestedDependenciesAsync(item);
        
        // Dispose the item itself
        if (item is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (item is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Gets the total number of tracked dependencies.
    /// </summary>
    public int GetDependencyCount()
    {
        lock (_lock)
        {
            return _dependencyTracker.GetDependencyCount();
        }
    }

    private IScopeManager CreateScopeManager(SharedType sharedType, string? key)
    {
        return sharedType switch
        {
            SharedType.None => new NoOpScopeManager(),
            SharedType.PerTestSession => new GlobalScopeManager(),
            SharedType.PerClass => new ClassScopeManager(),
            SharedType.PerAssembly => new AssemblyScopeManager(),
            SharedType.Keyed => new KeyedScopeManager(key ?? string.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(sharedType))
        };
    }

    private class NoOpScopeManager : IScopeManager
    {
        public T GetOrCreate<T>(Func<T> factory) => factory();
        public void IncrementUsage<T>() { }
        public Task<bool> TryDisposeAsync<T>(T item) => Task.FromResult(true);
    }

    private class GlobalScopeManager : IScopeManager
    {
        public T GetOrCreate<T>(Func<T> factory) => factory();
        public void IncrementUsage<T>() => TestDataContainer.IncrementGlobalUsage(typeof(T));
        public async Task<bool> TryDisposeAsync<T>(T item)
        {
            await TestDataContainer.ConsumeGlobalCount(item);
            return false; // TestDataContainer handles disposal
        }
    }

    private class ClassScopeManager : IScopeManager
    {
        public T GetOrCreate<T>(Func<T> factory) => factory();
        public void IncrementUsage<T>() { /* Handled by TestDataContainer */ }
        public Task<bool> TryDisposeAsync<T>(T item) => Task.FromResult(false); // TestDataContainer handles disposal
    }

    private class AssemblyScopeManager : IScopeManager
    {
        public T GetOrCreate<T>(Func<T> factory) => factory();
        public void IncrementUsage<T>() { /* Handled by TestDataContainer */ }
        public Task<bool> TryDisposeAsync<T>(T item) => Task.FromResult(false); // TestDataContainer handles disposal
    }

    private class KeyedScopeManager : IScopeManager
    {
        private readonly string _key;

        public KeyedScopeManager(string key)
        {
            _key = key;
        }

        public T GetOrCreate<T>(Func<T> factory) => factory();
        public void IncrementUsage<T>() => TestDataContainer.IncrementKeyUsage(_key, typeof(T));
        public async Task<bool> TryDisposeAsync<T>(T item)
        {
            await TestDataContainer.ConsumeKey(_key, typeof(T));
            return false; // TestDataContainer handles disposal
        }
    }
}