using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Interfaces;
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

    // Pre-allocated buffers to avoid LINQ allocations
    private const int DefaultReceiverBufferSize = 64;
    private readonly ITestStartEventReceiver[] _testStartReceiverBuffer = new ITestStartEventReceiver[DefaultReceiverBufferSize];
    private readonly ITestEndEventReceiver[] _testEndReceiverBuffer = new ITestEndEventReceiver[DefaultReceiverBufferSize];
    private readonly ITestSkippedEventReceiver[] _testSkippedReceiverBuffer = new ITestSkippedEventReceiver[DefaultReceiverBufferSize];
    private readonly ITestDiscoveryEventReceiver[] _testDiscoveryReceiverBuffer = new ITestDiscoveryEventReceiver[DefaultReceiverBufferSize];
    // Event batching for improved performance
    private readonly System.Collections.Concurrent.ConcurrentQueue<(TestContext Context, CancellationToken Token)> _pendingTestStartEvents = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<(TestContext Context, CancellationToken Token)> _pendingTestEndEvents = new();
    private readonly Timer? _batchProcessingTimer;

    // Track which assemblies/classes/sessions have had their "first" event invoked
    private GetOnlyDictionary<string, Task> _firstTestInAssemblyTasks = new();
    private GetOnlyDictionary<Type, Task> _firstTestInClassTasks = new();
    private GetOnlyDictionary<string, Task> _firstTestInSessionTasks = new();

    // Track remaining test counts for "last" events
    private readonly ConcurrentDictionary<string, int> _assemblyTestCounts = new();
    private readonly ConcurrentDictionary<Type, int> _classTestCounts = new();
    private int _sessionTestCount;

    public EventReceiverOrchestrator(TUnitFrameworkLogger logger)
    {
        _logger = logger;
        
        // Initialize batching timer for event processing optimization
        // Process batched events every 50ms for better throughput
        _batchProcessingTimer = new Timer(ProcessBatchedEvents, null, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
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
        // Try to collect receivers into pre-allocated buffer first
        var receiverCount = CollectReceiversIntoBuffer(context, _testStartReceiverBuffer);
        
        IReadOnlyList<ITestStartEventReceiver> receivers;
        if (receiverCount == -1)
        {
            // Buffer overflow, use fallback
            receivers = CollectReceiversFallback<ITestStartEventReceiver>(context);
        }
        else
        {
            // Create a list view of the buffer segment to avoid allocation
            receivers = new ArraySegment<ITestStartEventReceiver>(_testStartReceiverBuffer, 0, receiverCount);
        }

        // Filter scoped attributes
        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(receivers);

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
                try
                {
                    await receiver.OnTestStart(context).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Error in test start event receiver: {ex.Message}").ConfigureAwait(false);
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
        // Try to collect receivers into pre-allocated buffer first
        var receiverCount = CollectReceiversIntoBuffer(context, _testEndReceiverBuffer);
        
        IReadOnlyList<ITestEndEventReceiver> receivers;
        if (receiverCount == -1)
        {
            // Buffer overflow, use fallback
            receivers = CollectReceiversFallback<ITestEndEventReceiver>(context);
        }
        else
        {
            // Create a list view of the buffer segment to avoid allocation
            receivers = new ArraySegment<ITestEndEventReceiver>(_testEndReceiverBuffer, 0, receiverCount);
        }

        // Filter scoped attributes
        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(receivers);

        foreach (var receiver in filteredReceivers)
        {
            try
            {
                await receiver.OnTestEnd(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test end event receiver: {ex.Message}").ConfigureAwait(false);
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
        // Try to collect receivers into pre-allocated buffer first
        var receiverCount = CollectReceiversIntoBuffer(context, _testSkippedReceiverBuffer);
        
        IReadOnlyList<ITestSkippedEventReceiver> receivers;
        if (receiverCount == -1)
        {
            // Buffer overflow, use fallback
            receivers = CollectReceiversFallback<ITestSkippedEventReceiver>(context);
        }
        else
        {
            // Create a list view of the buffer segment to avoid allocation
            receivers = new ArraySegment<ITestSkippedEventReceiver>(_testSkippedReceiverBuffer, 0, receiverCount);
        }

        // Filter scoped attributes
        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(receivers);

        foreach (var receiver in filteredReceivers)
        {
            try
            {
                await receiver.OnTestSkipped(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in test skipped event receiver: {ex.Message}").ConfigureAwait(false);
            }
        }
    }

    public async ValueTask InvokeTestDiscoveryEventReceiversAsync(TestContext context, DiscoveredTestContext discoveredContext, CancellationToken cancellationToken)
    {
        var eventReceivers = context.GetEligibleEventObjects()
            .OfType<ITestDiscoveryEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        // Filter scoped attributes to ensure only the highest priority one of each type is invoked
        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(eventReceivers);

        foreach (var receiver in filteredReceivers.OrderBy(r => r.Order))
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

    public async ValueTask InvokeHookRegistrationEventReceiversAsync(HookRegisteredContext hookContext, CancellationToken cancellationToken)
    {
        // Get event receivers from the hook method's attributes
        var eventReceivers = hookContext.HookMethod.Attributes
            .OfType<IHookRegisteredEventReceiver>()
            .OrderBy(r => r.Order)
            .ToList();

        // Filter scoped attributes to ensure only the highest priority one of each type is invoked
        var filteredReceivers = ScopedAttributeFilter.FilterScopedAttributes(eventReceivers);

        foreach (var receiver in filteredReceivers.OrderBy(r => r.Order))
        {
            try
            {
                await receiver.OnHookRegistered(hookContext);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in hook registration event receiver: {ex.Message}");
            }
        }

        // Apply the timeout from the context back to the hook method
        if (hookContext.Timeout.HasValue && hookContext.HookMethod != null)
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
        
        // Dispose the global static property context after all tests complete
        if (TestSessionContext.GlobalStaticPropertyContext.Events.OnDispose != null)
        {
            try
            {
                foreach (var invocation in TestSessionContext.GlobalStaticPropertyContext.Events.OnDispose.InvocationList.OrderBy(x => x.Order))
                {
                    await invocation.InvokeAsync(TestSessionContext.GlobalStaticPropertyContext, context);
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error disposing global static property context: {ex.Message}");
            }
        }

        // Clear SharedType.PerTestSession cached instances when session ends
        TestDataContainer.ClearGlobalScope();
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

        // Clear SharedType.PerAssembly cached instances when assembly ends
        TestDataContainer.ClearAssemblyScope(context.TestDetails.ClassType.Assembly);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeLastTestInClassEventReceiversAsync(
        TestContext context,
        ClassHookContext classContext,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[EventReceiverOrchestrator] InvokeLastTestInClassEventReceiversAsync called for {classContext.ClassType.Name}");
        
        if (!_registry.HasLastTestInClassReceivers())
        {
            Console.WriteLine($"[EventReceiverOrchestrator] No last test in class receivers for {classContext.ClassType.Name}");
            return;
        }

        var classType = classContext.ClassType;
        var count = _classTestCounts.AddOrUpdate(classType, 0, (_, count) => count - 1);
        Console.WriteLine($"[EventReceiverOrchestrator] Class test count for {classType.Name} is now {count}");
        
        if (count == 0)
        {
            Console.WriteLine($"[EventReceiverOrchestrator] Last test in class {classType.Name} - disposing shared objects");
            // Dispose SharedType.PerClass objects BEFORE running After(Class) hooks
            TestDataContainer.ClearClassScope(classType);
            
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
        _firstTestInAssemblyTasks = new GetOnlyDictionary<string, Task>();
        _firstTestInClassTasks = new GetOnlyDictionary<Type, Task>();
        _firstTestInSessionTasks = new GetOnlyDictionary<string, Task>();

        foreach (var group in contexts.Where(c => c.ClassContext != null).GroupBy(c => c.ClassContext!.AssemblyContext.Assembly.GetName().FullName))
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
        try
        {
            await invoker(receiver);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Event receiver {receiver.GetType().Name} threw exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Efficiently collects event receivers of the specified type without LINQ allocations
    /// </summary>
    private int CollectReceiversIntoBuffer<T>(TestContext context, T[] buffer) where T : class, IEventReceiver
    {
        var count = 0;
        var eligibleObjects = context.GetEligibleEventObjects();
        
        foreach (var obj in eligibleObjects)
        {
            if (obj is T receiver)
            {
                if (count >= buffer.Length)
                {
                    // Buffer overflow - fall back to LINQ for simplicity
                    // This should be rare if buffer is sized appropriately
                    _logger.LogWarningAsync($"Receiver buffer overflow for type {typeof(T).Name}. Consider increasing buffer size.").ConfigureAwait(false);
                    return -1; // Signal to use fallback
                }
                
                buffer[count++] = receiver;
            }
        }
        
        // Sort by order in-place using Array.Sort for better performance than LINQ OrderBy
        if (count > 1)
        {
            Array.Sort(buffer, 0, count, Comparer<T>.Create((x, y) => x.Order.CompareTo(y.Order)));
        }
        
        return count;
    }

    /// <summary>
    /// Fallback method using LINQ when buffer is insufficient
    /// </summary>
    private List<T> CollectReceiversFallback<T>(TestContext context) where T : class, IEventReceiver
    {
        return context.GetEligibleEventObjects()
            .OfType<T>()
            .OrderBy(r => r.Order)
            .ToList();
    }

    /// <summary>
    /// Processes batched events for improved throughput
    /// </summary>
    private async void ProcessBatchedEvents(object? state)
    {
        // Process test start events in batch
        var startEventBatch = new List<(TestContext Context, CancellationToken Token)>();
        while (_pendingTestStartEvents.TryDequeue(out var startEvent) && startEventBatch.Count < 32)
        {
            startEventBatch.Add(startEvent);
        }
        
        if (startEventBatch.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                foreach (var (context, token) in startEventBatch)
                {
                    try
                    {
                        await InvokeTestStartEventReceiversCore(context, token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogErrorAsync($"Error processing batched test start event: {ex.Message}").ConfigureAwait(false);
                    }
                }
            });
        }

        // Process test end events in batch
        var endEventBatch = new List<(TestContext Context, CancellationToken Token)>();
        while (_pendingTestEndEvents.TryDequeue(out var endEvent) && endEventBatch.Count < 32)
        {
            endEventBatch.Add(endEvent);
        }
        
        if (endEventBatch.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                foreach (var (context, token) in endEventBatch)
                {
                    try
                    {
                        await InvokeTestEndEventReceiversCore(context, token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogErrorAsync($"Error processing batched test end event: {ex.Message}").ConfigureAwait(false);
                    }
                }
            });
        }
    }

    /// <summary>
    /// Adds test start event to batch queue for processing
    /// </summary>
    public void QueueTestStartEvent(TestContext context, CancellationToken cancellationToken)
    {
        _pendingTestStartEvents.Enqueue((context, cancellationToken));
    }

    /// <summary>
    /// Adds test end event to batch queue for processing
    /// </summary>
    public void QueueTestEndEvent(TestContext context, CancellationToken cancellationToken)
    {
        _pendingTestEndEvents.Enqueue((context, cancellationToken));
    }

    public void Dispose()
    {
        _batchProcessingTimer?.Dispose();
        _registry?.Dispose();
    }
}
