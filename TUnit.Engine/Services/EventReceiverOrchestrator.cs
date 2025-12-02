using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Engine.Events;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;
using TUnit.Engine.Utilities;

namespace TUnit.Engine.Services;

internal sealed class EventReceiverOrchestrator : IDisposable
{
    private readonly EventReceiverRegistry _registry = new();
    private readonly TUnitFrameworkLogger _logger;

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

    public EventReceiverOrchestrator(TUnitFrameworkLogger logger)
    {
        _logger = logger;
    }

    public void RegisterReceivers(TestContext context, CancellationToken cancellationToken)
    {
        var objectsToRegister = new List<object>();

        foreach (var obj in context.GetEligibleEventObjects())
        {
            if (_initializedObjects.Contains(obj))
            {
                continue;
            }

            if (_initializedObjects.Add(obj))
            {
                bool isFirstEventReceiver = obj is IFirstTestInTestSessionEventReceiver ||
                                           obj is IFirstTestInAssemblyEventReceiver ||
                                           obj is IFirstTestInClassEventReceiver;

                if (isFirstEventReceiver)
                {
                    var objType = obj.GetType();

                    if (_registeredFirstEventReceiverTypes.Contains(objType))
                    {
                        continue;
                    }

                    if (_registeredFirstEventReceiverTypes.Add(objType))
                    {
                        objectsToRegister.Add(obj);
                    }
                }
                else
                {
                    objectsToRegister.Add(obj);
                }
            }
        }

        if (objectsToRegister.Count > 0)
        {
            _registry.RegisterReceivers(objectsToRegister);
        }
    }


    // Fast-path checks with inlining
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeTestStartEventReceiversAsync(TestContext context, CancellationToken cancellationToken, EventReceiverStage? stage = null)
    {
        // Fast path - no allocation if no receivers
        if (!_registry.HasTestStartReceivers())
        {
            return;
        }

        await InvokeTestStartEventReceiversCore(context, cancellationToken, stage);
    }

    private async ValueTask InvokeTestStartEventReceiversCore(TestContext context, CancellationToken cancellationToken, EventReceiverStage? stage)
    {
        // Manual filtering and sorting instead of LINQ to avoid allocations
        var eligibleObjects = context.GetEligibleEventObjects();
        List<ITestStartEventReceiver>? receivers = null;

        foreach (var obj in eligibleObjects)
        {
            if (obj is ITestStartEventReceiver receiver)
            {
#if NET
                // Filter by stage if specified (only on .NET 8.0+ where Stage property exists)
                if (stage.HasValue && receiver.Stage != stage.Value)
                {
                    continue;
                }
#endif
                receivers ??= [];
                receivers.Add(receiver);
            }
        }

        if (receivers == null)
        {
            return;
        }

        // Manual sort instead of OrderBy
        receivers.Sort((a, b) => a.Order.CompareTo(b.Order));

        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(receivers);

        foreach (var receiver in filteredReceivers)
        {
            await receiver.OnTestStart(context);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<List<Exception>> InvokeTestEndEventReceiversAsync(TestContext context, CancellationToken cancellationToken, EventReceiverStage? stage = null)
    {
        if (!_registry.HasTestEndReceivers())
        {
            return [];
        }

        return await InvokeTestEndEventReceiversCore(context, cancellationToken, stage);
    }

    private async ValueTask<List<Exception>> InvokeTestEndEventReceiversCore(TestContext context, CancellationToken cancellationToken, EventReceiverStage? stage)
    {
        var exceptions = new List<Exception>();

        // Manual filtering and sorting instead of LINQ to avoid allocations
        var eligibleObjects = context.GetEligibleEventObjects();
        List<ITestEndEventReceiver>? receivers = null;

        foreach (var obj in eligibleObjects)
        {
            if (obj is ITestEndEventReceiver receiver)
            {
#if NET
                // Filter by stage if specified (only on .NET 8.0+ where Stage property exists)
                if (stage.HasValue && receiver.Stage != stage.Value)
                {
                    continue;
                }
#endif
                receivers ??= [];
                receivers.Add(receiver);
            }
        }

        if (receivers == null)
        {
            return exceptions;
        }

        // Manual sort instead of OrderBy
        receivers.Sort((a, b) => a.Order.CompareTo(b.Order));

        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(receivers);

        foreach (var receiver in filteredReceivers)
        {
            try
            {
                await receiver.OnTestEnd(context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test end event receiver: {ex.Message}");
                exceptions.Add(ex);
            }
        }

        return exceptions;
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
        // Manual filtering and sorting instead of LINQ to avoid allocations
        var eligibleObjects = context.GetEligibleEventObjects();
        List<ITestSkippedEventReceiver>? receivers = null;

        foreach (var obj in eligibleObjects)
        {
            if (obj is ITestSkippedEventReceiver receiver)
            {
                receivers ??= [];
                receivers.Add(receiver);
            }
        }

        if (receivers == null)
        {
            return;
        }

        // Manual sort instead of OrderBy
        receivers.Sort((a, b) => a.Order.CompareTo(b.Order));

        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(receivers);

        foreach (var receiver in filteredReceivers)
        {
            await receiver.OnTestSkipped(context);
        }
    }

    public async ValueTask InvokeTestDiscoveryEventReceiversAsync(TestContext context, DiscoveredTestContext discoveredContext, CancellationToken cancellationToken)
    {
        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ITestDiscoveryEventReceiver>();

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
    public ValueTask InvokeFirstTestInSessionEventReceiversAsync(
        TestContext context,
        TestSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasFirstTestInSessionReceivers())
        {
            return default;
        }

        var task = _firstTestInSessionTasks.GetOrAdd("session",
            _ => InvokeFirstTestInSessionEventReceiversCoreAsync(context, sessionContext, cancellationToken));
        return new ValueTask(task);
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
    public ValueTask InvokeFirstTestInAssemblyEventReceiversAsync(
        TestContext context,
        AssemblyHookContext assemblyContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasFirstTestInAssemblyReceivers())
        {
            return default;
        }

        var assemblyName = assemblyContext.Assembly.GetName().FullName ?? "";
        var task = _firstTestInAssemblyTasks.GetOrAdd(assemblyName,
            _ => InvokeFirstTestInAssemblyEventReceiversCoreAsync(context, assemblyContext, cancellationToken));
        return new ValueTask(task);
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
    public ValueTask InvokeFirstTestInClassEventReceiversAsync(
        TestContext context,
        ClassHookContext classContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasFirstTestInClassReceivers())
        {
            return default;
        }

        var classType = classContext.ClassType;
        var task = _firstTestInClassTasks.GetOrAdd(classType,
            _ => InvokeFirstTestInClassEventReceiversCoreAsync(context, classContext, cancellationToken));
        return new ValueTask(task);
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
        var contexts = allTestContexts as IList<TestContext> ?? allTestContexts.ToList();
        _sessionTestCount = contexts.Count;

        // Clear first-event tracking to ensure clean state for each test execution
        _firstTestInAssemblyTasks = new ThreadSafeDictionary<string, Task>();
        _firstTestInClassTasks = new ThreadSafeDictionary<Type, Task>();
        _firstTestInSessionTasks = new ThreadSafeDictionary<string, Task>();

        foreach (var group in contexts.GroupBy(c => c.ClassContext.AssemblyContext.Assembly.GetName().FullName))
        {
            var counter = _assemblyTestCounts.GetOrAdd(group.Key, static _ => new Counter());
            counter.Add(group.Count());
        }

        foreach (var group in contexts.GroupBy(c => c.ClassContext.ClassType))
        {
            var counter = _classTestCounts.GetOrAdd(group.Key, static _ => new Counter());
            counter.Add(group.Count());
        }
    }

    public void Dispose()
    {
        _registry.Dispose();
    }
}
