using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;
using TUnit.Engine.Events;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;
using TUnit.Engine.Utilities;

namespace TUnit.Engine.Services;

/// Optimized event receiver orchestrator with fast-path checks, batching, and lifecycle tracking
internal sealed class EventReceiverOrchestrator : IDisposable
{
    private readonly EventReceiverRegistry _registry = new();
    private readonly TUnitFrameworkLogger _logger;
    private readonly TrackableObjectGraphProvider _trackableObjectGraphProvider;

    // Track which assemblies/classes/sessions have had their "first" event invoked
    private ThreadSafeDictionary<string, Task> _firstTestInAssemblyTasks = new();
    private ThreadSafeDictionary<Type, Task> _firstTestInClassTasks = new();
    private ThreadSafeDictionary<string, Task> _firstTestInSessionTasks = new();

    // Track remaining test counts for "last" events
    private readonly ThreadSafeDictionary<string, Counter> _assemblyTestCounts = new();
    private readonly ThreadSafeDictionary<Type, Counter> _classTestCounts = new();
    private int _sessionTestCount;

    // Track which objects have already been initialized to avoid duplicates
    private readonly ConcurrentHashSet<object> _initializedObjects = new();

    // Track registered First event receiver types to avoid duplicate registrations
    private readonly ConcurrentHashSet<Type> _registeredFirstEventReceiverTypes = new();

    public EventReceiverOrchestrator(TUnitFrameworkLogger logger, TrackableObjectGraphProvider trackableObjectGraphProvider)
    {
        _logger = logger;
        _trackableObjectGraphProvider = trackableObjectGraphProvider;
    }

    public void RegisterReceivers(TestContext context, CancellationToken cancellationToken)
    {
        var eligibleObjects = context.GetEligibleEventObjects().ToArray();

        var objectsToRegister = new List<object>();

        foreach (var obj in eligibleObjects)
        {
            if (_initializedObjects.Add(obj)) // Add returns false if already present
            {
                // For First event receivers, only register one instance per type
                var objType = obj.GetType();
                bool isFirstEventReceiver = obj is IFirstTestInTestSessionEventReceiver ||
                                           obj is IFirstTestInAssemblyEventReceiver ||
                                           obj is IFirstTestInClassEventReceiver;

                if (isFirstEventReceiver)
                {
                    if (_registeredFirstEventReceiverTypes.Add(objType))
                    {
                        // First instance of this type, register it
                        objectsToRegister.Add(obj);
                    }
                    // else: Skip registration, we already have an instance of this type
                }
                else
                {
                    // Not a First event receiver, register normally
                    objectsToRegister.Add(obj);
                }
            }
        }

        if (objectsToRegister.Count > 0)
        {
            // Register only the objects that should be registered
            _registry.RegisterReceivers(objectsToRegister);
        }
    }


    // Fast-path checks with inlining
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeTestStartEventReceiversAsync(TestContext context, CancellationToken cancellationToken)
    {
        // Fast path - no allocation if no receivers
        if (!_registry.HasTestStartReceivers())
        {
            return;
        }

        await InvokeTestStartEventReceiversCore(context, cancellationToken);
    }

    private async ValueTask InvokeTestStartEventReceiversCore(TestContext context, CancellationToken cancellationToken)
    {
        // Filter scoped attributes - FilterScopedAttributes will materialize the collection
        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(
            context.GetEligibleEventObjects()
                .OfType<ITestStartEventReceiver>()
                .OrderBy(static r => r.Order));

        // Batch invocation for multiple receivers
        if (filteredReceivers.Count > 3)
        {
            await InvokeBatchedAsync(filteredReceivers.ToArray(), r => r.OnTestStart(context), cancellationToken);
        }
        else
        {
            // Sequential for small counts
            foreach (var receiver in filteredReceivers)
            {
                await receiver.OnTestStart(context);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeTestEndEventReceiversAsync(TestContext context, CancellationToken cancellationToken)
    {
        if (!_registry.HasTestEndReceivers())
        {
            return;
        }

        await InvokeTestEndEventReceiversCore(context, cancellationToken);
    }

    private async ValueTask InvokeTestEndEventReceiversCore(TestContext context, CancellationToken cancellationToken)
    {
        // Filter scoped attributes - FilterScopedAttributes will materialize the collection
        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(
            context.GetEligibleEventObjects()
                .OfType<ITestEndEventReceiver>()
                .OrderBy(static r => r.Order));

        foreach (var receiver in filteredReceivers)
        {
            try
            {
                await receiver.OnTestEnd(context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test end event receiver: {ex.Message}");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeTestSkippedEventReceiversAsync(TestContext context, CancellationToken cancellationToken)
    {
        if (!_registry.HasTestSkippedReceivers())
        {
            return;
        }

        await InvokeTestSkippedEventReceiversCore(context, cancellationToken);
    }

    private async ValueTask InvokeTestSkippedEventReceiversCore(TestContext context, CancellationToken cancellationToken)
    {
        // Filter scoped attributes - FilterScopedAttributes will materialize the collection
        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(
            context.GetEligibleEventObjects()
                .OfType<ITestSkippedEventReceiver>()
                .OrderBy(static r => r.Order));

        foreach (var receiver in filteredReceivers)
        {
            await receiver.OnTestSkipped(context);
        }
    }

    public async ValueTask InvokeTestDiscoveryEventReceiversAsync(TestContext context, DiscoveredTestContext discoveredContext, CancellationToken cancellationToken)
    {
        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ITestDiscoveryEventReceiver>()
            .OrderBy(static r => r.Order)
            .ToList();

        // Filter scoped attributes to ensure only the highest priority one of each type is invoked
        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(eventReceivers);

        foreach (var receiver in filteredReceivers.OrderBy(static r => r.Order))
        {
            await receiver.OnTestDiscovered(discoveredContext);
        }
    }

    public async ValueTask InvokeHookRegistrationEventReceiversAsync(HookRegisteredContext hookContext, CancellationToken cancellationToken)
    {
        // Filter scoped attributes to ensure only the highest priority one of each type is invoked
        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(
            hookContext.HookMethod.Attributes
                .OfType<IHookRegisteredEventReceiver>()
                .OrderBy(static r => r.Order));

        foreach (var receiver in filteredReceivers.OrderBy(static r => r.Order))
        {
            await receiver.OnHookRegistered(hookContext);
        }

        // Apply the timeout from the context back to the hook method
        if (hookContext is { Timeout: not null })
        {
            hookContext.HookMethod.Timeout = hookContext.Timeout;
        }
    }


    // First/Last event methods with fast-path checks
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeFirstTestInSessionEventReceiversAsync(
        TestContext context,
        TestSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasFirstTestInSessionReceivers())
        {
            return;
        }

        // Use GetOrAdd to ensure exactly one task is created per session and all tests await it
        var task = _firstTestInSessionTasks.GetOrAdd("session",
            _ => InvokeFirstTestInSessionEventReceiversCoreAsync(context, sessionContext, cancellationToken));
        await task;
    }

    private async Task InvokeFirstTestInSessionEventReceiversCoreAsync(
        TestContext context,
        TestSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<IFirstTestInTestSessionEventReceiver>();

        foreach (var receiver in receivers)
        {
            await receiver.OnFirstTestInTestSession(sessionContext, context);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeFirstTestInAssemblyEventReceiversAsync(
        TestContext context,
        AssemblyHookContext assemblyContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasFirstTestInAssemblyReceivers())
        {
            return;
        }

        var assemblyName = assemblyContext.Assembly.GetName().FullName ?? "";
        // Use GetOrAdd to ensure exactly one task is created per assembly and all tests await it
        var task = _firstTestInAssemblyTasks.GetOrAdd(assemblyName,
            _ => InvokeFirstTestInAssemblyEventReceiversCoreAsync(context, assemblyContext, cancellationToken));
        await task;
    }

    private async Task InvokeFirstTestInAssemblyEventReceiversCoreAsync(
        TestContext context,
        AssemblyHookContext assemblyContext,
        CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<IFirstTestInAssemblyEventReceiver>();

        foreach (var receiver in receivers)
        {
            await receiver.OnFirstTestInAssembly(assemblyContext, context);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeFirstTestInClassEventReceiversAsync(
        TestContext context,
        ClassHookContext classContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasFirstTestInClassReceivers())
        {
            return;
        }

        var classType = classContext.ClassType;
        // Use GetOrAdd to ensure exactly one task is created per class and all tests await it
        var task = _firstTestInClassTasks.GetOrAdd(classType,
            _ => InvokeFirstTestInClassEventReceiversCoreAsync(context, classContext, cancellationToken));
        await task;
    }

    private async Task InvokeFirstTestInClassEventReceiversCoreAsync(
        TestContext context,
        ClassHookContext classContext,
        CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<IFirstTestInClassEventReceiver>();

        foreach (var receiver in receivers)
        {
            await receiver.OnFirstTestInClass(classContext, context);
        }
    }

    // Last event methods
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeLastTestInSessionEventReceiversAsync(
        TestContext context,
        TestSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasLastTestInSessionReceivers())
        {
            return;
        }

        if (Interlocked.Decrement(ref _sessionTestCount) == 0)
        {
            await InvokeLastTestInSessionEventReceiversCore(context, sessionContext, cancellationToken);
        }
    }

    private async ValueTask InvokeLastTestInSessionEventReceiversCore(
        TestContext context,
        TestSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<ILastTestInTestSessionEventReceiver>();

        foreach (var receiver in receivers)
        {
            try
            {
                await receiver.OnLastTestInTestSession(sessionContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in session event receiver: {ex.Message}");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeLastTestInAssemblyEventReceiversAsync(
        TestContext context,
        AssemblyHookContext assemblyContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasLastTestInAssemblyReceivers())
        {
            return;
        }

        var assemblyName = assemblyContext.Assembly.GetName().FullName ?? "";

        var assemblyCount = _assemblyTestCounts.GetOrAdd(assemblyName, static _ => new Counter()).Decrement();

        if (assemblyCount == 0)
        {
            await InvokeLastTestInAssemblyEventReceiversCore(context, assemblyContext, cancellationToken);
        }
    }

    private async ValueTask InvokeLastTestInAssemblyEventReceiversCore(
        TestContext context,
        AssemblyHookContext assemblyContext,
        CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<ILastTestInAssemblyEventReceiver>();

        foreach (var receiver in receivers)
        {
            try
            {
                await receiver.OnLastTestInAssembly(assemblyContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in assembly event receiver: {ex.Message}");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeLastTestInClassEventReceiversAsync(
        TestContext context,
        ClassHookContext classContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasLastTestInClassReceivers())
        {
            return;
        }

        var classType = classContext.ClassType;

        var classCount = _classTestCounts.GetOrAdd(classType, static _ => new Counter()).Decrement();

        if (classCount == 0)
        {
            await InvokeLastTestInClassEventReceiversCore(context, classContext, cancellationToken);
        }
    }

    private async ValueTask InvokeLastTestInClassEventReceiversCore(
        TestContext context,
        ClassHookContext classContext,
        CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<ILastTestInClassEventReceiver>();

        foreach (var receiver in receivers)
        {
            try
            {
                await receiver.OnLastTestInClass(classContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in class event receiver: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Initialize test counts for first/last event receivers
    /// </summary>
    public void InitializeTestCounts(IEnumerable<TestContext> allTestContexts)
    {
        var contexts = allTestContexts.ToList();
        _sessionTestCount = contexts.Count;

        // Clear first-event tracking to ensure clean state for each test execution
        _firstTestInAssemblyTasks = new ThreadSafeDictionary<string, Task>();
        _firstTestInClassTasks = new ThreadSafeDictionary<Type, Task>();
        _firstTestInSessionTasks = new ThreadSafeDictionary<string, Task>();

        foreach (var group in contexts.GroupBy(c => c.ClassContext.AssemblyContext.Assembly.GetName().FullName))
        {
            var counter = _assemblyTestCounts.GetOrAdd(group.Key, static _ => new Counter());

            for (var i = 0; i < group.Count(); i++)
            {
                counter.Increment();
            }
        }

        foreach (var group in contexts.GroupBy(c => c.ClassContext.ClassType))
        {
            var counter = _classTestCounts.GetOrAdd(group.Key, static _ => new Counter());

            for (var i = 0; i < group.Count(); i++)
            {
                counter.Increment();
            }
        }
    }

    /// <summary>
    /// Batch multiple receiver invocations
    /// </summary>
    private async ValueTask InvokeBatchedAsync<T>(
        T[] receivers,
        Func<T, ValueTask> invoker,
        CancellationToken cancellationToken) where T : IEventReceiver
    {
        // Parallelize for larger counts
        var tasks = new Task[receivers.Length];
        for (var i = 0; i < receivers.Length; i++)
        {
            var receiver = receivers[i];
            tasks[i] = InvokeReceiverAsync(receiver, invoker, cancellationToken);
        }

        await Task.WhenAll(tasks);
    }

    private async Task InvokeReceiverAsync<T>(
        T receiver,
        Func<T, ValueTask> invoker,
        CancellationToken cancellationToken) where T : IEventReceiver
    {
        await invoker(receiver);
    }

    public void Dispose()
    {
        _registry.Dispose();
    }
}
