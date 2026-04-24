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
    private readonly Lock _testSessionLock = new();

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

    [UnconditionalSuppressMessage("Trimming", "IL2077",
        Justification = "Type parameter is annotated at the method boundary and the closure invokes ExecuteBeforeClassHooksAsync which requires the same annotation.")]
    public ValueTask GetOrCreateBeforeClassTask(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass,
        HookExecutor hookExecutor,
        CancellationToken cancellationToken)
    {
        // Lock-free fast path avoids allocating a closure on the common cache-hit case.
        if (_beforeClassTasks.TryGetValue(testClass, out var existingTask))
        {
            return new ValueTask(existingTask);
        }

        // ThreadSafeDictionary<,> internally uses Lazy<T> with ExecutionAndPublication,
        // guaranteeing single-execution per key without serializing unrelated classes
        // behind a shared lock.
        var task = _beforeClassTasks.GetOrAdd(
            testClass,
            _ => hookExecutor.ExecuteBeforeClassHooksAsync(testClass, cancellationToken).AsTask());
        return new ValueTask(task);
    }
}
