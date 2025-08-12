using System.Runtime.CompilerServices;

namespace TUnit.Core.Data;

/// <summary>
/// Tracks nested dependencies between objects for proper disposal ordering.
/// </summary>
internal class DependencyTracker
{
    // Use ConditionalWeakTable to avoid preventing garbage collection
    private readonly ConditionalWeakTable<object, List<IDisposableReference>> _dependencies = new();
    private readonly object _lock = new();
    private int _dependencyCount;

    /// <summary>
    /// Registers a nested dependency relationship.
    /// </summary>
    /// <typeparam name="T">The type of the child object.</typeparam>
    /// <param name="parent">The parent object.</param>
    /// <param name="child">The child object that the parent depends on.</param>
    /// <param name="scopeManager">The scope manager responsible for the child object.</param>
    public void RegisterDependency<T>(object parent, T child, IScopeManager scopeManager)
    {
        if (parent == null || child == null || scopeManager == null)
        {
            return;
        }

        lock (_lock)
        {
            var dependencies = _dependencies.GetOrCreateValue(parent);
            var wasEmpty = dependencies.Count == 0;
            dependencies.Add(new ScopedReference<T>(child, scopeManager));

            // Only increment count if this is the first dependency for this parent
            if (wasEmpty)
            {
                _dependencyCount++;
            }
        }
    }

    /// <summary>
    /// Disposes all nested dependencies for the specified parent object.
    /// </summary>
    /// <param name="parent">The parent object whose dependencies should be disposed.</param>
    /// <returns>A task representing the disposal operation.</returns>
    public async Task DisposeNestedDependenciesAsync(object parent)
    {
        if (parent == null)
        {
            return;
        }

        List<IDisposableReference>? dependencies = null;
        var hadDependencies = false;

        lock (_lock)
        {
            if (_dependencies.TryGetValue(parent, out dependencies))
            {
                _dependencies.Remove(parent);
                hadDependencies = true;
                _dependencyCount--;
            }
        }

        if (dependencies != null && hadDependencies)
        {
            // Dispose dependencies in parallel for better performance
            var disposalTasks = dependencies.Select(dep => dep.DisposeAsync().AsTask());
            await Task.WhenAll(disposalTasks).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the number of objects that have nested dependencies.
    /// </summary>
    /// <returns>The count of parent objects with dependencies.</returns>
    public int GetDependencyCount()
    {
        lock (_lock)
        {
            return _dependencyCount;
        }
    }
}
