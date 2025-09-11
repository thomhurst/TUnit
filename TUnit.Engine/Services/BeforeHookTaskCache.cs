using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Engine.Services;

/// <summary>
/// Responsible for caching Before hook tasks to ensure they run only once.
/// Follows Single Responsibility Principle - only handles task caching.
/// </summary>
public sealed class BeforeHookTaskCache : IDisposable
{
    // Cached Before hook tasks to ensure they run only once
    private readonly ConcurrentDictionary<Type, Task> _beforeClassTasks = new();
    private readonly ConcurrentDictionary<Assembly, Task> _beforeAssemblyTasks = new();
    private Task? _beforeTestSessionTask;

    public Task GetOrCreateBeforeTestSessionTask(Func<Task> taskFactory)
    {
        return _beforeTestSessionTask ??= taskFactory();
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

    public void Dispose()
    {
        _beforeClassTasks.Clear();
        _beforeAssemblyTasks.Clear();
        _beforeTestSessionTask = null;
    }
}