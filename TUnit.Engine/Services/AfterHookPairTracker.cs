using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Data;

namespace TUnit.Engine.Services;

/// <summary>
/// Responsible for ensuring After hooks run even when tests are cancelled.
/// When a Before hook completes, this tracker registers the corresponding After hook
/// to run on cancellation, guaranteeing cleanup even if the test is aborted.
/// Follows Single Responsibility Principle - only handles After hook pairing and cancellation registration.
/// </summary>
internal sealed class AfterHookPairTracker
{
    // Cached After hook tasks to ensure they run only once (prevent double execution)
    private readonly ThreadSafeDictionary<Type, Task<List<Exception>>> _afterClassTasks = new();
    private readonly ThreadSafeDictionary<Assembly, Task<List<Exception>>> _afterAssemblyTasks = new();
    private Task<List<Exception>>? _afterTestSessionTask;
    private readonly object _testSessionLock = new();
    private readonly object _classLock = new();

    // Track cancellation registrations for cleanup
    private readonly ConcurrentBag<CancellationTokenRegistration> _registrations = [];

    /// <summary>
    /// Registers Session After hooks to run on cancellation or normal completion.
    /// Ensures After hooks run exactly once even if called both ways.
    /// </summary>
    public void RegisterAfterTestSessionHook(
        CancellationToken cancellationToken,
        Func<ValueTask<List<Exception>>> afterHookExecutor)
    {
        // Register callback to run After hook on cancellation
        var registration = cancellationToken.Register(() =>
        {
            // Use sync-over-async here because CancellationToken.Register requires Action (not Func<Task>)
            // Fire-and-forget is acceptable here - exceptions will be collected when hooks run normally
            _ = GetOrCreateAfterTestSessionTask(afterHookExecutor);
        });

        _registrations.Add(registration);
    }

    /// <summary>
    /// Registers Assembly After hooks to run on cancellation or normal completion.
    /// Ensures After hooks run exactly once even if called both ways.
    /// </summary>
    public void RegisterAfterAssemblyHook(
        Assembly assembly,
        CancellationToken cancellationToken,
        Func<Assembly, ValueTask<List<Exception>>> afterHookExecutor)
    {
        var registration = cancellationToken.Register(() =>
        {
            _ = GetOrCreateAfterAssemblyTask(assembly, afterHookExecutor);
        });

        _registrations.Add(registration);
    }

    /// <summary>
    /// Registers Class After hooks to run on cancellation or normal completion.
    /// Ensures After hooks run exactly once even if called both ways.
    /// </summary>
    public void RegisterAfterClassHook(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass,
        HookExecutor hookExecutor,
        CancellationToken cancellationToken)
    {
        var registration = cancellationToken.Register(() =>
        {
            _ = GetOrCreateAfterClassTask(testClass, hookExecutor, CancellationToken.None);
        });

        _registrations.Add(registration);
    }

    /// <summary>
    /// Gets or creates the After Test Session task, ensuring it runs only once.
    /// Thread-safe using double-checked locking.
    /// Returns the exceptions from hook execution.
    /// </summary>
    public ValueTask<List<Exception>> GetOrCreateAfterTestSessionTask(Func<ValueTask<List<Exception>>> taskFactory)
    {
        if (_afterTestSessionTask != null)
        {
            return new ValueTask<List<Exception>>(_afterTestSessionTask);
        }

        lock (_testSessionLock)
        {
            if (_afterTestSessionTask == null)
            {
                _afterTestSessionTask = taskFactory().AsTask();
            }
            return new ValueTask<List<Exception>>(_afterTestSessionTask);
        }
    }

    /// <summary>
    /// Gets or creates the After Assembly task for the specified assembly.
    /// Thread-safe using ThreadSafeDictionary.
    /// Returns the exceptions from hook execution.
    /// </summary>
    public ValueTask<List<Exception>> GetOrCreateAfterAssemblyTask(Assembly assembly, Func<Assembly, ValueTask<List<Exception>>> taskFactory)
    {
        var task = _afterAssemblyTasks.GetOrAdd(assembly, a => taskFactory(a).AsTask());
        return new ValueTask<List<Exception>>(task);
    }

    /// <summary>
    /// Gets or creates the After Class task for the specified test class.
    /// Thread-safe using double-checked locking.
    /// Returns the exceptions from hook execution.
    /// </summary>
    public ValueTask<List<Exception>> GetOrCreateAfterClassTask(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass,
        HookExecutor hookExecutor,
        CancellationToken cancellationToken)
    {
        if (_afterClassTasks.TryGetValue(testClass, out var existingTask))
        {
            return new ValueTask<List<Exception>>(existingTask);
        }

        lock (_classLock)
        {
            if (_afterClassTasks.TryGetValue(testClass, out existingTask))
            {
                return new ValueTask<List<Exception>>(existingTask);
            }

            // Call ExecuteAfterClassHooksAsync directly with the annotated testClass
            // The factory ignores the key since we've already created the task with the annotated type
            var newTask = hookExecutor.ExecuteAfterClassHooksAsync(testClass, cancellationToken).AsTask();
            _afterClassTasks.GetOrAdd(testClass, _ => newTask);
            return new ValueTask<List<Exception>>(newTask);
        }
    }

    /// <summary>
    /// Disposes all cancellation token registrations.
    /// Should be called at the end of test execution to clean up resources.
    /// </summary>
    public void Dispose()
    {
        foreach (var registration in _registrations)
        {
            registration.Dispose();
        }
    }
}
