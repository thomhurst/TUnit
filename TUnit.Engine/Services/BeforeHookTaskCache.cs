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
    private readonly GetOnlyDictionary<Type, Task> _beforeClassTasks = new();
    private readonly GetOnlyDictionary<Assembly, Task> _beforeAssemblyTasks = new();
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
}
