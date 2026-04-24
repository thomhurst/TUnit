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
    private readonly Lock _testSessionLock = new();

    // Ensure only the first call to RegisterAfterTestSessionHook registers a callback.
    // Subsequent calls (e.g. from per-test timeout tokens) are ignored so that
    // a test timeout cannot prematurely trigger session-level After hooks.
    // Use Interlocked.CompareExchange to avoid TOCTOU race where two threads both
    // observe 0 and both proceed to register.
    private int _sessionHookRegistered;

    // Per-test callers would otherwise register one CancellationTokenRegistration per test
    // for every assembly/class — 10k tests across 5 assemblies = 50k redundant registrations.
    // First registration wins; subsequent calls short-circuit.
    //
    // Safety of single registration: The CancellationToken passed in is always the session-scoped
    // token (or a derivative from Parallel.ForEachAsync which is itself linked to the session CT).
    // Per-test timeout tokens are applied inside TestExecutor via TimeoutHelper.CreateLinkedTokenSource
    // scoped to the test body only — they never reach this method. Therefore when the session cancels,
    // the first test's registered CT still fires regardless of whether that test has completed.
    private readonly ConcurrentHashSet<Assembly> _assemblyHookRegistered = new();
    private readonly ConcurrentHashSet<Type> _classHookRegistered = new();

    // Track cancellation registrations for cleanup
    private readonly ConcurrentBag<CancellationTokenRegistration> _registrations = [];

    /// <summary>
    /// Registers Session After hooks to run on cancellation or normal completion.
    /// Only the first call registers a callback; subsequent calls are no-ops.
    /// This prevents per-test timeout tokens from prematurely firing session hooks.
    /// </summary>
    public void RegisterAfterTestSessionHook(
        CancellationToken sessionCancellationToken,
        Func<ValueTask<List<Exception>>> afterHookExecutor)
    {
        if (Interlocked.CompareExchange(ref _sessionHookRegistered, 1, 0) != 0)
        {
            return;
        }

        // Register callback to run After hook on cancellation
        var registration = sessionCancellationToken.Register(static state =>
        {
            var (pairTracker, afterHookExecutor) = ((AfterHookPairTracker, Func<ValueTask<List<Exception>>>))state!;
            // Use sync-over-async here because CancellationToken.Register requires Action (not Func<Task>)
            // Fire-and-forget is acceptable here - exceptions will be collected when hooks run normally
            _ = pairTracker.GetOrCreateAfterTestSessionTask(afterHookExecutor);
        }, (this, afterHookExecutor));

        _registrations.Add(registration);
    }

    /// <summary>
    /// Registers Assembly After hooks to run on cancellation or normal completion.
    /// Ensures After hooks run exactly once even if called both ways.
    /// </summary>
    /// <param name="sessionCancellationToken">
    /// MUST be the session-scoped token (or a derivative linked to it). Only the first caller per
    /// assembly registers a callback; subsequent calls short-circuit. Passing a narrower per-test
    /// token would cause After hooks to miss cancellation when later tests' tokens fire.
    /// </param>
    public void RegisterAfterAssemblyHook(
        Assembly assembly,
        CancellationToken sessionCancellationToken,
        Func<Assembly, ValueTask<List<Exception>>> afterHookExecutor)
    {
        if (!_assemblyHookRegistered.Add(assembly))
        {
            return;
        }

        var registration = sessionCancellationToken.Register(static state =>
        {
            var (pairTracker, assembly, afterHookExecutor) = ((AfterHookPairTracker, Assembly, Func<Assembly, ValueTask<List<Exception>>>))state!;
            _ = pairTracker.GetOrCreateAfterAssemblyTask(assembly, afterHookExecutor);
        }, (this, assembly, afterHookExecutor));

        _registrations.Add(registration);
    }

    /// <summary>
    /// Registers Class After hooks to run on cancellation or normal completion.
    /// Ensures After hooks run exactly once even if called both ways.
    /// </summary>
    /// <param name="sessionCancellationToken">
    /// MUST be the session-scoped token (or a derivative linked to it). Only the first caller per
    /// class registers a callback; subsequent calls short-circuit. Passing a narrower per-test
    /// token would cause After hooks to miss cancellation when later tests' tokens fire.
    /// </param>
     [UnconditionalSuppressMessage("Trimming", "IL2077",
            Justification = "Type parameter is annotated at the method boundary.")]
    public void RegisterAfterClassHook(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass,
        HookExecutor hookExecutor,
        CancellationToken sessionCancellationToken)
    {
        if (!_classHookRegistered.Add(testClass))
        {
            return;
        }

        var registration = sessionCancellationToken.Register(static state =>
        {
            var (pairTracker, testClass, hookExecutor) = ((AfterHookPairTracker, Type, HookExecutor))state!;
            _ = pairTracker.GetOrCreateAfterClassTask(testClass, hookExecutor, CancellationToken.None);
        }, (this, testClass, hookExecutor));

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
    /// Thread-safe via ThreadSafeDictionary's per-key Lazy initialization, which
    /// guarantees single-execution without serializing unrelated classes behind a shared lock.
    /// Returns the exceptions from hook execution.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2077",
        Justification = "Type parameter is annotated at the method boundary and the closure invokes ExecuteAfterClassHooksAsync which requires the same annotation.")]
    public ValueTask<List<Exception>> GetOrCreateAfterClassTask(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type testClass,
        HookExecutor hookExecutor,
        CancellationToken cancellationToken)
    {
        // Lock-free fast path avoids allocating a closure on the common cache-hit case.
        if (_afterClassTasks.TryGetValue(testClass, out var existingTask))
        {
            return new ValueTask<List<Exception>>(existingTask);
        }

        // ThreadSafeDictionary<,> internally uses Lazy<T> with ExecutionAndPublication,
        // guaranteeing single-execution per key without serializing unrelated classes
        // behind a shared lock.
        var task = _afterClassTasks.GetOrAdd(
            testClass,
            _ => hookExecutor.ExecuteAfterClassHooksAsync(testClass, cancellationToken).AsTask());
        return new ValueTask<List<Exception>>(task);
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
