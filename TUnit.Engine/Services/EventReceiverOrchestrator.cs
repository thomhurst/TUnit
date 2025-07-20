using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Events;
using TUnit.Engine.Extensions;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

/// <summary>
/// Optimized event receiver orchestrator with fast-path checks and batching support
/// </summary>
internal sealed class EventReceiverOrchestrator : IDisposable
{
    private readonly EventReceiverRegistry _registry = new();
    private readonly TUnitFrameworkLogger _logger;

    // Track which assemblies/classes/sessions have had their "first" event invoked
    private readonly ConcurrentDictionary<string, bool> _firstTestInAssemblyInvoked = new();
    private readonly ConcurrentDictionary<Type, bool> _firstTestInClassInvoked = new();
    private int _firstTestInSessionInvoked;

    // Track remaining test counts for "last" events
    private readonly ConcurrentDictionary<string, int> _assemblyTestCounts = new();
    private readonly ConcurrentDictionary<Type, int> _classTestCounts = new();
    private int _sessionTestCount;

    public EventReceiverOrchestrator(TUnitFrameworkLogger logger)
    {
        _logger = logger;
    }

    public async ValueTask InitializeAllEligibleObjectsAsync(TestContext context, CancellationToken cancellationToken)
    {
        var eligibleObjects = context.GetEligibleEventObjects().ToArray();

        // Register all event receivers for fast lookup
        _registry.RegisterReceivers(eligibleObjects);

        foreach (var obj in eligibleObjects)
        {
            try
            {
                await ObjectInitializer.InitializeAsync(obj, cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error initializing object of type {obj.GetType().Name}: {ex.Message}");
            }
        }
    }

    // Conditional compilation for event logging
    [Conditional("ENABLE_TEST_EVENTS")]
    private static void LogEventInvocation(string eventName, string testName)
    {
        Console.WriteLine($"[Event] {eventName} for test {testName}");
    }

    // Fast-path checks with inlining
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeTestStartEventReceiversAsync(TestContext context, CancellationToken cancellationToken)
    {
        LogEventInvocation("TestStart", context.TestDetails.TestName);

        // Fast path - no allocation if no receivers
        if (!_registry.HasTestStartReceivers())
        {
            return;
        }

        await InvokeTestStartEventReceiversCore(context, cancellationToken);
    }

    private async ValueTask InvokeTestStartEventReceiversCore(TestContext context, CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<ITestStartEventReceiver>();

        // Sort by order once
        if (receivers.Length > 1)
        {
            Array.Sort(receivers, (a, b) => a.Order.CompareTo(b.Order));
        }

        // Batch invocation for multiple receivers
        if (receivers.Length > 3)
        {
            await InvokeBatchedAsync(receivers, r => r.OnTestStart(context), cancellationToken);
        }
        else
        {
            // Sequential for small counts
            foreach (var receiver in receivers)
            {
                try
                {
                    await receiver.OnTestStart(context);
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Error in test start event receiver: {ex.Message}");
                }
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
        var receivers = _registry.GetReceiversOfType<ITestEndEventReceiver>();

        if (receivers.Length > 1)
        {
            Array.Sort(receivers, (a, b) => a.Order.CompareTo(b.Order));
        }

        foreach (var receiver in receivers)
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
        var receivers = _registry.GetReceiversOfType<ITestSkippedEventReceiver>();

        if (receivers.Length > 1)
        {
            Array.Sort(receivers, (a, b) => a.Order.CompareTo(b.Order));
        }

        foreach (var receiver in receivers)
        {
            try
            {
                await receiver.OnTestSkipped(context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test skipped event receiver: {ex.Message}");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeTestRegisteredEventReceiversAsync(TestContext context, CancellationToken cancellationToken)
    {
        if (!_registry.HasTestRegisteredReceivers())
        {
            return;
        }

        await InvokeTestRegisteredEventReceiversCore(context, cancellationToken);
    }

    private async ValueTask InvokeTestRegisteredEventReceiversCore(TestContext context, CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<ITestRegisteredEventReceiver>();
        var registeredContext = new TestRegisteredContext(context)
        {
            DiscoveredTest = context.InternalDiscoveredTest!
        };

        foreach (var receiver in receivers)
        {
            try
            {
                await receiver.OnTestRegistered(registeredContext);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test registered event receiver: {ex.Message}");
            }
        }
    }

    public async ValueTask InvokeTestDiscoveryEventReceiversAsync(TestContext context, DiscoveredTestContext discoveredContext, CancellationToken cancellationToken)
    {
        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ITestDiscoveryEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        foreach (var receiver in eventReceivers)
        {
            try
            {
                await receiver.OnTestDiscovered(discoveredContext);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test discovery event receiver: {ex.Message}");
            }
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

        if (Interlocked.CompareExchange(ref _firstTestInSessionInvoked, 1, 0) == 0)
        {
            await InvokeFirstTestInSessionEventReceiversCore(context, sessionContext, cancellationToken);
        }
    }

    private async ValueTask InvokeFirstTestInSessionEventReceiversCore(
        TestContext context,
        TestSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<IFirstTestInTestSessionEventReceiver>();

        foreach (var receiver in receivers)
        {
            try
            {
                await receiver.OnFirstTestInTestSession(sessionContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in first test in session event receiver: {ex.Message}");
            }
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

        var assemblyName = assemblyContext.Assembly.FullName ?? "";
        if (_firstTestInAssemblyInvoked.TryAdd(assemblyName, true))
        {
            await InvokeFirstTestInAssemblyEventReceiversCore(context, assemblyContext, cancellationToken);
        }
    }

    private async ValueTask InvokeFirstTestInAssemblyEventReceiversCore(
        TestContext context,
        AssemblyHookContext assemblyContext,
        CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<IFirstTestInAssemblyEventReceiver>();

        foreach (var receiver in receivers)
        {
            try
            {
                await receiver.OnFirstTestInAssembly(assemblyContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in first test in assembly event receiver: {ex.Message}");
            }
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
        if (_firstTestInClassInvoked.TryAdd(classType, true))
        {
            await InvokeFirstTestInClassEventReceiversCore(context, classContext, cancellationToken);
        }
    }

    private async ValueTask InvokeFirstTestInClassEventReceiversCore(
        TestContext context,
        ClassHookContext classContext,
        CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<IFirstTestInClassEventReceiver>();

        foreach (var receiver in receivers)
        {
            try
            {
                await receiver.OnFirstTestInClass(classContext, context);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in first test in class event receiver: {ex.Message}");
            }
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

        var assemblyName = assemblyContext.Assembly.FullName ?? "";
        if (_assemblyTestCounts.AddOrUpdate(assemblyName, 0, (_, count) => count - 1) == 0)
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
        if (_classTestCounts.AddOrUpdate(classType, 0, (_, count) => count - 1) == 0)
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

        foreach (var group in contexts.Where(c => c.ClassContext != null).GroupBy(c => c.ClassContext!.AssemblyContext.Assembly.FullName))
        {
            if (group.Key != null)
            {
                _assemblyTestCounts[group.Key] = group.Count();
            }
        }

        foreach (var group in contexts.Where(c => c.ClassContext != null).GroupBy(c => c.ClassContext!.ClassType))
        {
            if (group.Key != null)
            {
                _classTestCounts[group.Key] = group.Count();
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
        for (int i = 0; i < receivers.Length; i++)
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
        try
        {
            await invoker(receiver);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Event receiver {receiver.GetType().Name} threw exception: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _registry?.Dispose();
    }
}
