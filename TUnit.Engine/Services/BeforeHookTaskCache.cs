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
    private readonly object _classLock = new();

    public ValueTask GetOrCreateBeforeTestSessionTask(Func<CancellationToken, ValueTask> taskFactory, CancellationToken cancellationToken)
    {
        if (_beforeTestSessionTask != null)
        {
            return new ValueTask(_beforeTestSessionTask);
        }

        lock (_testSessionLock)
        {
            if (_beforeTestSessionTask == null)
            {
                _beforeTestSessionTask = taskFactory(cancellationToken).AsTask();
            }
            return new ValueTask(_beforeTestSessionTask);
        }
    }

    public ValueTask GetOrCreateBeforeAssemblyTask(Assembly assembly, Func<Assembly, CancellationToken, ValueTask> taskFactory, CancellationToken cancellationToken)
    {
        var task = _beforeAssemblyTasks.GetOrAdd(assembly, a => taskFactory(a, cancellationToken).AsTask());
        return new ValueTask(task);
    }

    public ValueTask GetOrCreateBeforeClassTask(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass,
        HookExecutor hookExecutor,
        CancellationToken cancellationToken)
    {
        if (_beforeClassTasks.TryGetValue(testClass, out var existingTask))
        {
            return new ValueTask(existingTask);
        }

        lock (_classLock)
        {
            if (_beforeClassTasks.TryGetValue(testClass, out existingTask))
            {
                return new ValueTask(existingTask);
            }

            // Call ExecuteBeforeClassHooksAsync directly with the annotated testClass
            // The factory ignores the key since we've already created the task with the annotated type
            var newTask = hookExecutor.ExecuteBeforeClassHooksAsync(testClass, cancellationToken).AsTask();
            _beforeClassTasks.GetOrAdd(testClass, _ => newTask);
            return new ValueTask(newTask);
        }
    }
}
