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

    public Task GetOrCreateBeforeTestSessionTask(Func<Task> taskFactory)
    {
        if (_beforeTestSessionTask != null)
        {
            return _beforeTestSessionTask;
        }

        lock (_testSessionLock)
        {
            // Double-check after acquiring lock
            if (_beforeTestSessionTask == null)
            {
                _beforeTestSessionTask = taskFactory();
            }
            return _beforeTestSessionTask;
        }
    }

    public Task GetOrCreateBeforeAssemblyTask(Assembly assembly, Func<Assembly, Task> taskFactory)
    {
        return _beforeAssemblyTasks.GetOrAdd(assembly, taskFactory);
    }

    public Task GetOrCreateBeforeClassTask(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass, Func<Type, Task> taskFactory)
    {
        return _beforeClassTasks.GetOrAdd(testClass, taskFactory);
    }
}
