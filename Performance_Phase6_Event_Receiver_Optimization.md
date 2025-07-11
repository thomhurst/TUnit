# Phase 6: Event Receiver Optimization

## Overview
Optimize event receiver invocations to eliminate overhead when no receivers are registered. Add fast-path checks and batch event processing where possible.

## Goals
1. Zero overhead when no event receivers registered
2. Reduce invocation cost for common scenarios
3. Enable batched event processing
4. Maintain full compatibility with existing receivers

## Current Problem
- Event receiver methods called for every test regardless of registration
- Multiple virtual calls per test (6+ events)
- No fast-path for empty receiver lists
- Each event processed individually

## Design Principles
- **SRP**: Event dispatch separate from event handling
- **KISS**: Simple presence checks before invocation
- **DRY**: Centralized event batching logic
- **AOT Compatible**: No dynamic event generation

## Implementation Plan

### Step 1: Create Event Receiver Registry
Create `TUnit.Engine/Events/EventReceiverRegistry.cs`:

```csharp
using System.Runtime.CompilerServices;

namespace TUnit.Engine.Events;

/// <summary>
/// Fast registry for event receiver presence checks
/// </summary>
internal sealed class EventReceiverRegistry
{
    // Bit flags for fast checking
    [Flags]
    private enum EventTypes
    {
        None = 0,
        TestRegistered = 1 << 0,
        TestStart = 1 << 1,
        TestEnd = 1 << 2,
        TestSkipped = 1 << 3,
        FirstTestInSession = 1 << 4,
        LastTestInSession = 1 << 5,
        FirstTestInAssembly = 1 << 6,
        LastTestInAssembly = 1 << 7,
        FirstTestInClass = 1 << 8,
        LastTestInClass = 1 << 9,
        All = ~0
    }
    
    private volatile EventTypes _registeredEvents = EventTypes.None;
    private readonly Dictionary<Type, IEventReceiver[]> _receiversByType = new();
    private readonly ReaderWriterLockSlim _lock = new();
    
    /// <summary>
    /// Register an event receiver
    /// </summary>
    public void RegisterReceiver(IEventReceiver receiver)
    {
        _lock.EnterWriteLock();
        try
        {
            var type = receiver.GetType();
            UpdateEventFlags(receiver);
            
            if (_receiversByType.TryGetValue(type, out var existing))
            {
                var newArray = new IEventReceiver[existing.Length + 1];
                existing.CopyTo(newArray, 0);
                newArray[^1] = receiver;
                _receiversByType[type] = newArray;
            }
            else
            {
                _receiversByType[type] = [receiver];
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Fast check if any receivers registered for event type
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTestStartReceivers() => (_registeredEvents & EventTypes.TestStart) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTestEndReceivers() => (_registeredEvents & EventTypes.TestEnd) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTestSkippedReceivers() => (_registeredEvents & EventTypes.TestSkipped) != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasAnyReceivers() => _registeredEvents != EventTypes.None;
    
    /// <summary>
    /// Get receivers of specific type (for invocation)
    /// </summary>
    public IEventReceiver[] GetReceiversOfType<T>() where T : IEventReceiver
    {
        _lock.EnterReadLock();
        try
        {
            return _receiversByType.TryGetValue(typeof(T), out var receivers) 
                ? receivers 
                : Array.Empty<IEventReceiver>();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    private void UpdateEventFlags(IEventReceiver receiver)
    {
        // Set flags based on implemented interfaces
        if (receiver is ITestStartEventReceiver)
            _registeredEvents |= EventTypes.TestStart;
        if (receiver is ITestEndEventReceiver)
            _registeredEvents |= EventTypes.TestEnd;
        if (receiver is ITestSkippedEventReceiver)
            _registeredEvents |= EventTypes.TestSkipped;
        // ... etc for other event types
    }
}
```

### Step 2: Optimized Event Receiver Orchestrator
Update `TUnit.Engine/Services/EventReceiverOrchestrator.cs`:

```csharp
internal sealed class EventReceiverOrchestrator
{
    private readonly EventReceiverRegistry _registry = new();
    private readonly TUnitFrameworkLogger _logger;
    
    // Fast-path checks
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask InvokeTestStartEventReceiversAsync(
        TestContext context, 
        CancellationToken cancellationToken)
    {
        // Fast path - no allocation if no receivers
        if (!_registry.HasTestStartReceivers())
            return;
            
        await InvokeTestStartEventReceiversCore(context, cancellationToken);
    }
    
    private async ValueTask InvokeTestStartEventReceiversCore(
        TestContext context, 
        CancellationToken cancellationToken)
    {
        var receivers = _registry.GetReceiversOfType<ITestStartEventReceiver>();
        
        // Batch invocation for multiple receivers
        if (receivers.Length > 1)
        {
            await InvokeBatchedAsync(receivers, r => ((ITestStartEventReceiver)r).OnTestStart(context), cancellationToken);
        }
        else if (receivers.Length == 1)
        {
            await ((ITestStartEventReceiver)receivers[0]).OnTestStart(context);
        }
    }
    
    /// <summary>
    /// Batch multiple receiver invocations
    /// </summary>
    private async ValueTask InvokeBatchedAsync(
        IEventReceiver[] receivers,
        Func<IEventReceiver, ValueTask> invoker,
        CancellationToken cancellationToken)
    {
        // For small counts, sequential is fine
        if (receivers.Length <= 3)
        {
            foreach (var receiver in receivers)
            {
                await invoker(receiver);
            }
            return;
        }
        
        // For larger counts, parallelize
        var tasks = new Task[receivers.Length];
        for (int i = 0; i < receivers.Length; i++)
        {
            var receiver = receivers[i];
            tasks[i] = InvokeReceiverAsync(receiver, invoker, cancellationToken);
        }
        
        await Task.WhenAll(tasks);
    }
    
    private async Task InvokeReceiverAsync(
        IEventReceiver receiver,
        Func<IEventReceiver, ValueTask> invoker,
        CancellationToken cancellationToken)
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
}
```

### Step 3: Event Batching for High-Frequency Events
Create `TUnit.Engine/Events/EventBatcher.cs`:

```csharp
internal sealed class EventBatcher<TEvent> : IDisposable where TEvent : class
{
    private readonly Channel<TEvent> _eventChannel;
    private readonly Func<IReadOnlyList<TEvent>, ValueTask> _batchProcessor;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _shutdownCts = new();
    
    public EventBatcher(
        Func<IReadOnlyList<TEvent>, ValueTask> batchProcessor,
        int batchSize = 100,
        TimeSpan maxBatchDelay = default)
    {
        _batchProcessor = batchProcessor;
        _eventChannel = Channel.CreateUnbounded<TEvent>();
        
        _processingTask = ProcessBatchesAsync(
            batchSize, 
            maxBatchDelay == default ? TimeSpan.FromMilliseconds(10) : maxBatchDelay,
            _shutdownCts.Token);
    }
    
    public async ValueTask EnqueueEventAsync(TEvent evt)
    {
        await _eventChannel.Writer.WriteAsync(evt);
    }
    
    private async Task ProcessBatchesAsync(
        int batchSize, 
        TimeSpan maxDelay,
        CancellationToken cancellationToken)
    {
        var batch = new List<TEvent>(batchSize);
        using var timer = new PeriodicTimer(maxDelay);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            // Collect batch
            var timerTask = timer.WaitForNextTickAsync(cancellationToken).AsTask();
            
            while (batch.Count < batchSize)
            {
                var readTask = _eventChannel.Reader.ReadAsync(cancellationToken).AsTask();
                var completedTask = await Task.WhenAny(readTask, timerTask);
                
                if (completedTask == timerTask)
                {
                    break; // Time to process batch
                }
                
                batch.Add(await readTask);
            }
            
            // Process batch if we have events
            if (batch.Count > 0)
            {
                await _batchProcessor(batch);
                batch.Clear();
            }
        }
    }
    
    public void Dispose()
    {
        _eventChannel.Writer.TryComplete();
        _shutdownCts.Cancel();
        _processingTask.Wait(TimeSpan.FromSeconds(5));
        _shutdownCts.Dispose();
    }
}
```

### Step 4: Conditional Event Compilation
Add compile-time event removal:

```csharp
// In EventReceiverOrchestrator.cs
internal sealed class EventReceiverOrchestrator
{
    // Conditional compilation for event overhead
    [Conditional("ENABLE_TEST_EVENTS")]
    private static void LogEventInvocation(string eventName, string testName)
    {
        Console.WriteLine($"[Event] {eventName} for test {testName}");
    }
    
    public async ValueTask InvokeTestStartEventReceiversAsync(
        TestContext context, 
        CancellationToken cancellationToken)
    {
        LogEventInvocation("TestStart", context.TestName);
        
        if (!_registry.HasTestStartReceivers())
            return;
            
        await InvokeTestStartEventReceiversCore(context, cancellationToken);
    }
}
```

### Step 5: Event Receiver Caching
Cache receiver lookups:

```csharp
internal sealed class EventReceiverCache
{
    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        public Type ReceiverType { get; init; }
        public Type TestClassType { get; init; }
        
        public bool Equals(CacheKey other) =>
            ReceiverType == other.ReceiverType && 
            TestClassType == other.TestClassType;
            
        public override int GetHashCode() =>
            HashCode.Combine(ReceiverType, TestClassType);
    }
    
    private readonly ConcurrentDictionary<CacheKey, IEventReceiver[]> _cache = new();
    
    public IEventReceiver[] GetApplicableReceivers<T>(
        Type testClassType,
        Func<Type, IEventReceiver[]> factory) where T : IEventReceiver
    {
        var key = new CacheKey 
        { 
            ReceiverType = typeof(T), 
            TestClassType = testClassType 
        };
        
        return _cache.GetOrAdd(key, _ => factory(testClassType));
    }
}
```

### Step 6: Optimize First/Last Event Tracking
Improve first/last test detection:

```csharp
internal sealed class TestCountTracker
{
    private readonly ConcurrentDictionary<string, TestCountInfo> _counts = new();
    
    private sealed class TestCountInfo
    {
        private int _total;
        private int _executed;
        
        public void SetTotal(int total) => _total = total;
        
        public bool IsFirst() => Interlocked.Increment(ref _executed) == 1;
        
        public bool IsLast() => _executed == _total;
    }
    
    public void InitializeCounts(string key, int totalTests)
    {
        _counts.GetOrAdd(key, _ => new TestCountInfo()).SetTotal(totalTests);
    }
    
    public (bool isFirst, bool isLast) CheckTestPosition(string key)
    {
        if (!_counts.TryGetValue(key, out var info))
            return (false, false);
            
        return (info.IsFirst(), info.IsLast());
    }
}
```

## Testing Strategy
1. Verify zero overhead with no receivers
2. Test batching under high load
3. Ensure events fire in correct order
4. Validate error handling in receivers

## Success Metrics
- 95%+ reduction in overhead when no receivers
- 50%+ reduction in invocation cost with receivers
- Sub-microsecond fast-path checks
- Improved test throughput

## Risks and Mitigations
- **Risk**: Event ordering violations
  - **Mitigation**: Maintain sequential guarantees per test
- **Risk**: Receiver exceptions affecting tests
  - **Mitigation**: Isolate receiver failures
- **Risk**: Memory leaks from event queuing
  - **Mitigation**: Bounded channels and timeouts

## AOT Compatibility Notes
- No dynamic event creation
- All receiver types known at compile
- Conditional compilation AOT-safe
- Generic constraints preserved

## Performance Considerations
- Inline fast-path checks
- Array-based receiver storage
- Lock-free reads where possible
- CPU cache-friendly data layout

## Next Steps
After implementation:
1. Profile event overhead reduction
2. Monitor receiver invocation patterns
3. Consider compile-time receiver registration
4. Begin integration testing of all phases