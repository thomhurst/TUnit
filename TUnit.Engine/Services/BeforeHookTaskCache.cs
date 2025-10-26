using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Data;

namespace TUnit.Engine.Services;

/// <summary>
/// Responsible for caching Before hook tasks to ensure they run only once.
/// Follows Single Responsibility Principle - only handles task caching.
/// </summary>
internal sealed class BeforeHookTaskCache
{
    // Cached Before hook tasks to ensure they run only once
    private readonly ThreadSafeDictionary<Type, Task> _beforeClassTasks = new();
    private readonly ThreadSafeDictionary<Assembly, Task> _beforeAssemblyTasks = new();
    private Task? _beforeTestSessionTask;
    private readonly object _testSessionLock = new();

    /// <summary>
    /// Gets or creates the Before Test Session task. Returns ValueTask for optimal performance
    /// when the cached task is already completed. Must be awaited exactly once per call.
    /// </summary>
    public ValueTask GetOrCreateBeforeTestSessionTask(Func<ValueTask> taskFactory)
    {
        if (_beforeTestSessionTask != null)
        {
            // Fast path: return completed task wrapped in ValueTask (no allocation)
            return new ValueTask(_beforeTestSessionTask);
        }

        lock (_testSessionLock)
        {
            // Double-check after acquiring lock
            if (_beforeTestSessionTask == null)
            {
                _beforeTestSessionTask = taskFactory().AsTask();
            }
            return new ValueTask(_beforeTestSessionTask);
        }
    }

    /// <summary>
    /// Gets or creates the Before Assembly task. Returns ValueTask for optimal performance
    /// when the cached task is already completed. Must be awaited exactly once per call.
    /// </summary>
    public ValueTask GetOrCreateBeforeAssemblyTask(Assembly assembly, Func<Assembly, ValueTask> taskFactory)
    {
        // Cache stores Task (already completed on subsequent calls)
        // Wrap in ValueTask - compiler optimizes completed task case
        var task = _beforeAssemblyTasks.GetOrAdd(assembly, a => taskFactory(a).AsTask());
        return new ValueTask(task);
    }

    /// <summary>
    /// Gets or creates the Before Class task. Returns ValueTask for optimal performance
    /// when the cached task is already completed. Must be awaited exactly once per call.
    /// </summary>
    public ValueTask GetOrCreateBeforeClassTask(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, Func<Type, ValueTask> taskFactory)
    {
        // Cache stores Task (already completed on subsequent calls)
        // Wrap in ValueTask - compiler optimizes completed task case
        var task = _beforeClassTasks.GetOrAdd(testClass, t => taskFactory(t).AsTask());
        return new ValueTask(task);
    }
}
