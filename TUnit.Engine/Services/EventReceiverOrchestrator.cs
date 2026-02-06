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
    private readonly ConcurrentDictionary<string, Counter> _assemblyTestCounts = new();
    private readonly ConcurrentDictionary<Type, Counter> _classTestCounts = new();

    // Accessed from multiple threads via Interlocked to ensure atomic updates
    // and correct visibility across threads (prevents data races on the "last test" check).
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
        var vlb = new ValueListBuilder<object>([null, null, null, null]);

        foreach (var obj in context.GetEligibleEventObjects())
        {
            // Use single TryAdd operation instead of Contains + Add
            if (!_initializedObjects.Add(obj))
            {
                continue;
            }

            bool isFirstEventReceiver = obj is IFirstTestInTestSessionEventReceiver ||
                                       obj is IFirstTestInAssemblyEventReceiver ||
                                       obj is IFirstTestInClassEventReceiver;

            if (isFirstEventReceiver)
            {
                var objType = obj.GetType();

                // Use single TryAdd operation instead of Contains + Add
                if (!_registeredFirstEventReceiverTypes.Add(objType))
                {
                    continue;
                }
            }

            // Defer list allocation until actually needed
            vlb.Append(obj);
        }

        if (vlb.Length > 0)
        {
            _registry.RegisterReceivers(vlb.AsSpan());
        }
        vlb.Dispose();
    }


    // Fast-path checks with inlining
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask InvokeTestStartEventReceiversAsync(TestContext context, CancellationToken cancellationToken, EventReceiverStage? stage = null)
    {
        // Fast path - no allocation if no receivers
        if (!_registry.HasTestStartReceivers())
        {
            return ValueTask.CompletedTask;
        }

        return InvokeTestStartEventReceiversCore(context, cancellationToken, stage);
    }

    private async ValueTask InvokeTestStartEventReceiversCore(TestContext context, CancellationToken cancellationToken, EventReceiverStage? stage)
    {
        // Use pre-computed receivers (already filtered by stage, sorted, and scoped-attribute filtered)
#if NET
        if (stage.HasValue)
        {
            var receivers = context.GetTestStartReceivers(stage.Value);
            foreach (var receiver in receivers)
            {
                await receiver.OnTestStart(context);
            }
        }
        else
        {
            // No stage specified - invoke both Early and Late receivers in order
            var earlyReceivers = context.GetTestStartReceivers(EventReceiverStage.Early);
            foreach (var receiver in earlyReceivers)
            {
                await receiver.OnTestStart(context);
            }

            var lateReceivers = context.GetTestStartReceivers(EventReceiverStage.Late);
            foreach (var receiver in lateReceivers)
            {
                await receiver.OnTestStart(context);
            }
        }
#else
        var receivers = context.GetTestStartReceivers();
        foreach (var receiver in receivers)
        {
            await receiver.OnTestStart(context);
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<IReadOnlyList<Exception>> InvokeTestEndEventReceiversAsync(TestContext context, CancellationToken cancellationToken, EventReceiverStage? stage = null)
    {
        if (!_registry.HasTestEndReceivers())
        {
            return new ValueTask<IReadOnlyList<Exception>>([]);
        }
#if NET
        if (stage.HasValue)
        {
            var receivers = context.GetTestEndReceivers(stage.Value);
            if (receivers.Length == 0)
            {
                return new ValueTask<IReadOnlyList<Exception>>([]);
            }
        }
        else
        {
            var earlyReceivers = context.GetTestEndReceivers(EventReceiverStage.Early);
            var lateReceivers = context.GetTestEndReceivers(EventReceiverStage.Late);

            if (earlyReceivers.Length == 0 && lateReceivers.Length == 0)
            {
                return new ValueTask<IReadOnlyList<Exception>>([]);
            }
        }
#endif

        return InvokeTestEndEventReceiversCore(context, cancellationToken, stage);
    }

    private async ValueTask<IReadOnlyList<Exception>> InvokeTestEndEventReceiversCore(TestContext context, CancellationToken cancellationToken, EventReceiverStage? stage)
    {
        // Defer exception list allocation until actually needed
        List<Exception>? exceptions = null;

        // Use pre-computed receivers (already filtered by stage, sorted, and scoped-attribute filtered)
#if NET
        if (stage.HasValue)
        {
            var receivers = context.GetTestEndReceivers(stage.Value);
            foreach (var receiver in receivers)
            {
                try
                {
                    await receiver.OnTestEnd(context);
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Error in test end event receiver: {ex.Message}");
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }
        }
        else
        {
            // No stage specified - invoke both Early and Late receivers in order
            var earlyReceivers = context.GetTestEndReceivers(EventReceiverStage.Early);
            foreach (var receiver in earlyReceivers)
            {
                try
                {
                    await receiver.OnTestEnd(context);
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Error in test end event receiver: {ex.Message}");
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }

            var lateReceivers = context.GetTestEndReceivers(EventReceiverStage.Late);
            foreach (var receiver in lateReceivers)
            {
                try
                {
                    await receiver.OnTestEnd(context);
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Error in test end event receiver: {ex.Message}");
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }
        }
#else
        var receivers = context.GetTestEndReceivers();
        foreach (var receiver in receivers)
        {
            try
            {
                await receiver.OnTestEnd(context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test end event receiver: {ex.Message}");
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }
#endif

        return exceptions == null ? Array.Empty<Exception>() : exceptions;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask InvokeTestSkippedEventReceiversAsync(TestContext context, CancellationToken cancellationToken)
    {
        if (!_registry.HasTestSkippedReceivers())
        {
            return ValueTask.CompletedTask;
        }

        return InvokeTestSkippedEventReceiversCore(context, cancellationToken);
    }

    private async ValueTask InvokeTestSkippedEventReceiversCore(TestContext context, CancellationToken cancellationToken)
    {
        // Use pre-computed receivers (already filtered, sorted, and scoped-attribute filtered)
        var receivers = context.GetTestSkippedReceivers();

        if (receivers.Length == 0)
        {
            return;
        }

        foreach (var receiver in receivers)
        {
            await receiver.OnTestSkipped(context);
        }
    }

    public Task InvokeTestDiscoveryEventReceiversAsync(TestContext context, DiscoveredTestContext discoveredContext, CancellationToken cancellationToken)
    {
        // Use pre-computed receivers (already filtered, sorted, and scoped-attribute filtered)
        var receivers = context.GetTestDiscoveryReceivers();

        if(receivers.Length == 0)
        {
            return Task.CompletedTask;
        }

        return InvokeTestDiscoveryEventReceiversCoreAsync(receivers, discoveredContext);
    }

    private static async Task InvokeTestDiscoveryEventReceiversCoreAsync(ITestDiscoveryEventReceiver[] receivers, DiscoveredTestContext discoveredContext)
    {
        foreach (var receiver in receivers)
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
    public ValueTask InvokeLastTestInSessionEventReceiversAsync(
        TestContext context,
        TestSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasLastTestInSessionReceivers())
        {
            return ValueTask.CompletedTask;
        }

        if (Interlocked.Decrement(ref _sessionTestCount) == 0)
        {
            return InvokeLastTestInSessionEventReceiversCore(context, sessionContext, cancellationToken);
        }

        return ValueTask.CompletedTask;
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
    public ValueTask InvokeLastTestInAssemblyEventReceiversAsync(
        TestContext context,
        AssemblyHookContext assemblyContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasLastTestInAssemblyReceivers())
        {
            return ValueTask.CompletedTask;
        }

        var assemblyName = assemblyContext.Assembly.GetName().FullName ?? "";

        var assemblyCount = _assemblyTestCounts.GetOrAdd(assemblyName, static _ => new Counter()).Decrement();

        if (assemblyCount == 0)
        {
            return InvokeLastTestInAssemblyEventReceiversCore(context, assemblyContext, cancellationToken);
        }

        return ValueTask.CompletedTask;
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
    public ValueTask InvokeLastTestInClassEventReceiversAsync(
        TestContext context,
        ClassHookContext classContext,
        CancellationToken cancellationToken)
    {
        if (!_registry.HasLastTestInClassReceivers())
        {
            return ValueTask.CompletedTask;
        }

        var classType = classContext.ClassType;

        var classCount = _classTestCounts.GetOrAdd(classType, static _ => new Counter()).Decrement();

        if (classCount == 0)
        {
            return InvokeLastTestInClassEventReceiversCore(context, classContext, cancellationToken);
        }

        return ValueTask.CompletedTask;
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
        Interlocked.Exchange(ref _sessionTestCount, contexts.Count);

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
        // No longer need to dispose _registry - it no longer uses ReaderWriterLockSlim
    }
}
