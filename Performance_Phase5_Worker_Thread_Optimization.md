# Phase 5: Worker Thread Optimization

## Overview
Replace inefficient polling with proper synchronization primitives in the DAG test scheduler. Eliminate Task.Delay(10) busy-waiting and improve CPU efficiency.

## Goals
1. Eliminate CPU waste from polling
2. Reduce test execution latency
3. Improve work distribution efficiency
4. Maintain thread safety and correctness

## Current Problem
- Worker threads use Task.Delay(10) when no work available
- 10ms delays add up across multiple workers
- CPU cycles wasted in polling loops
- Work stealing only attempted after delays

## Design Principles
- **SRP**: Workers focus on execution, coordinator handles distribution
- **KISS**: Use standard synchronization primitives
- **DRY**: Single work notification mechanism
- **AOT Compatible**: No dynamic threading constructs

## Implementation Plan

### Step 1: Create Work Notification System
Create `TUnit.Engine/Scheduling/WorkNotificationSystem.cs`:

```csharp
using System.Threading.Channels;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Efficient work notification system for test workers
/// </summary>
internal sealed class WorkNotificationSystem : IDisposable
{
    private readonly Channel<WorkNotification> _workChannel;
    private readonly SemaphoreSlim _workAvailable;
    private readonly CancellationTokenSource _shutdownSource = new();
    
    public WorkNotificationSystem(int maxPendingNotifications = 1000)
    {
        // Bounded channel prevents runaway memory usage
        _workChannel = Channel.CreateBounded<WorkNotification>(
            new BoundedChannelOptions(maxPendingNotifications)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
            
        _workAvailable = new SemaphoreSlim(0);
    }
    
    /// <summary>
    /// Notify workers that new work is available
    /// </summary>
    public async ValueTask NotifyWorkAvailableAsync(
        WorkNotification notification,
        CancellationToken cancellationToken = default)
    {
        await _workChannel.Writer.WriteAsync(notification, cancellationToken);
        _workAvailable.Release();
    }
    
    /// <summary>
    /// Wait for work to become available
    /// </summary>
    public async ValueTask<WorkNotification?> WaitForWorkAsync(
        CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _shutdownSource.Token);
            
        try
        {
            await _workAvailable.WaitAsync(linkedCts.Token);
            
            if (_workChannel.Reader.TryRead(out var notification))
            {
                return notification;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        
        return null;
    }
    
    /// <summary>
    /// Complete work notifications
    /// </summary>
    public void CompleteNotifications()
    {
        _workChannel.Writer.TryComplete();
    }
    
    public void Dispose()
    {
        _shutdownSource.Cancel();
        _shutdownSource.Dispose();
        _workAvailable.Dispose();
    }
}

/// <summary>
/// Notification that work is available
/// </summary>
internal readonly struct WorkNotification
{
    public WorkSource Source { get; init; }
    public int Priority { get; init; }
    
    public enum WorkSource
    {
        GlobalQueue,
        LocalQueue,
        NewlyReady,
        Stolen
    }
}
```

### Step 2: Enhanced Work Stealing Queue
Update `TUnit.Engine/Scheduling/WorkStealingQueue.cs`:

```csharp
internal sealed class WorkStealingQueue<T> where T : class
{
    private readonly ConcurrentDeque<T> _items = new();
    private readonly WorkNotificationSystem? _notificationSystem;
    private volatile int _count;
    
    public WorkStealingQueue(WorkNotificationSystem? notificationSystem = null)
    {
        _notificationSystem = notificationSystem;
    }
    
    public int Count => _count;
    
    public async ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default)
    {
        _items.PushBottom(item);
        Interlocked.Increment(ref _count);
        
        // Notify workers if configured
        if (_notificationSystem != null)
        {
            await _notificationSystem.NotifyWorkAvailableAsync(
                new WorkNotification { Source = WorkNotification.WorkSource.LocalQueue },
                cancellationToken);
        }
    }
    
    public bool TryDequeue(out T? item)
    {
        item = _items.PopBottom();
        if (item != null)
        {
            Interlocked.Decrement(ref _count);
            return true;
        }
        return false;
    }
    
    public bool TrySteal(out T? item)
    {
        item = _items.PopTop();
        if (item != null)
        {
            Interlocked.Decrement(ref _count);
            return true;
        }
        return false;
    }
}
```

### Step 3: Optimized DAG Scheduler
Update `TUnit.Engine/Scheduling/DagTestScheduler.cs`:

```csharp
private async Task ExecuteTestsAsync(
    Dictionary<string, TestExecutionState> graph,
    ITestExecutor executor,
    CancellationToken cancellationToken)
{
    using var notificationSystem = new WorkNotificationSystem();
    var readyQueue = new ConcurrentQueue<TestExecutionState>();
    var completionTracker = new TestCompletionTracker(graph, readyQueue, notificationSystem);
    
    // Process pre-failed tests
    foreach (var state in graph.Values.Where(s => s.Test.State == TestState.Failed))
    {
        state.State = TestState.Failed;
        await completionTracker.OnTestCompletedAsync(state);
    }
    
    // Enqueue initial ready tests
    foreach (var state in graph.Values.Where(s => s.RemainingDependencies == 0 && s.Test.State != TestState.Failed))
    {
        readyQueue.Enqueue(state);
        await notificationSystem.NotifyWorkAvailableAsync(
            new WorkNotification { Source = WorkNotification.WorkSource.GlobalQueue });
    }
    
    // Start optimized workers
    var workers = CreateOptimizedWorkers(
        readyQueue, graph, executor, completionTracker, 
        notificationSystem, cancellationToken);
    
    try
    {
        await Task.WhenAll(workers);
    }
    finally
    {
        notificationSystem.CompleteNotifications();
    }
}

private Task[] CreateOptimizedWorkers(
    ConcurrentQueue<TestExecutionState> readyQueue,
    Dictionary<string, TestExecutionState> graph,
    ITestExecutor executor,
    TestCompletionTracker completionTracker,
    WorkNotificationSystem notificationSystem,
    CancellationToken cancellationToken)
{
    var workerCount = _parallelismStrategy.CurrentParallelism;
    var workers = new Task[workerCount];
    var workStealingQueues = new WorkStealingQueue<TestExecutionState>[workerCount];
    
    for (var i = 0; i < workerCount; i++)
    {
        workStealingQueues[i] = new WorkStealingQueue<TestExecutionState>(notificationSystem);
    }
    
    for (var i = 0; i < workerCount; i++)
    {
        var workerId = i;
        workers[i] = Task.Run(async () =>
        {
            await OptimizedWorkerLoopAsync(
                workerId,
                readyQueue,
                workStealingQueues,
                graph,
                executor,
                completionTracker,
                notificationSystem,
                cancellationToken);
        }, cancellationToken);
    }
    
    return workers;
}

private async Task OptimizedWorkerLoopAsync(
    int workerId,
    ConcurrentQueue<TestExecutionState> globalQueue,
    WorkStealingQueue<TestExecutionState>[] workStealingQueues,
    Dictionary<string, TestExecutionState> graph,
    ITestExecutor executor,
    TestCompletionTracker completionTracker,
    WorkNotificationSystem notificationSystem,
    CancellationToken cancellationToken)
{
    var localQueue = workStealingQueues[workerId];
    var consecutiveEmptyAttempts = 0;
    
    while (!cancellationToken.IsCancellationRequested)
    {
        TestExecutionState? state = null;
        
        // Try to get work without waiting
        if (TryGetWork(workerId, localQueue, globalQueue, workStealingQueues, out state))
        {
            consecutiveEmptyAttempts = 0;
            await ExecuteTestWithTimeoutAsync(state, executor, completionTracker, cancellationToken);
        }
        else if (completionTracker.AllTestsCompleted)
        {
            // All work done
            break;
        }
        else
        {
            // Wait for work notification
            consecutiveEmptyAttempts++;
            
            // Use exponential backoff for spurious wakeups
            var timeout = TimeSpan.FromMilliseconds(Math.Min(100, consecutiveEmptyAttempts * 10));
            
            var notification = await notificationSystem.WaitForWorkAsync(cancellationToken)
                .AsTask()
                .WaitAsync(timeout, cancellationToken);
                
            if (notification == null && completionTracker.AllTestsCompleted)
            {
                break;
            }
        }
    }
}

private bool TryGetWork(
    int workerId,
    WorkStealingQueue<TestExecutionState> localQueue,
    ConcurrentQueue<TestExecutionState> globalQueue,
    WorkStealingQueue<TestExecutionState>[] allQueues,
    out TestExecutionState? state)
{
    // Try local queue first (fastest)
    if (localQueue.TryDequeue(out state))
        return true;
        
    // Then global queue
    if (globalQueue.TryDequeue(out state))
        return true;
        
    // Finally try stealing (most expensive)
    return TryStealWork(workerId, allQueues, out state);
}
```

### Step 4: Update Test Completion Tracker
Notify when new tests become ready:

```csharp
internal sealed class TestCompletionTracker
{
    private readonly WorkNotificationSystem _notificationSystem;
    
    public async Task OnTestCompletedAsync(TestExecutionState completedTest)
    {
        // Existing completion logic...
        
        // Check dependents
        foreach (var dependentId in completedTest.Dependents)
        {
            if (_graph.TryGetValue(dependentId, out var dependent))
            {
                var remaining = Interlocked.Decrement(ref dependent.RemainingDependencies);
                if (remaining == 0)
                {
                    _readyQueue.Enqueue(dependent);
                    
                    // Notify workers immediately
                    await _notificationSystem.NotifyWorkAvailableAsync(
                        new WorkNotification 
                        { 
                            Source = WorkNotification.WorkSource.NewlyReady,
                            Priority = dependent.Test.Priority 
                        });
                }
            }
        }
        
        // Track completion...
    }
}
```

### Step 5: Add Work Batching
Reduce notification overhead by batching:

```csharp
internal sealed class BatchedWorkNotifier : IDisposable
{
    private readonly WorkNotificationSystem _notificationSystem;
    private readonly Timer _batchTimer;
    private int _pendingNotifications;
    
    public BatchedWorkNotifier(WorkNotificationSystem notificationSystem)
    {
        _notificationSystem = notificationSystem;
        _batchTimer = new Timer(FlushNotifications, null, Timeout.Infinite, Timeout.Infinite);
    }
    
    public void NotifyWorkAvailable()
    {
        if (Interlocked.Increment(ref _pendingNotifications) == 1)
        {
            // First notification, start batch timer
            _batchTimer.Change(TimeSpan.FromMicroseconds(100), Timeout.InfiniteTimeSpan);
        }
    }
    
    private async void FlushNotifications(object? state)
    {
        var count = Interlocked.Exchange(ref _pendingNotifications, 0);
        if (count > 0)
        {
            // Send single notification for batch
            await _notificationSystem.NotifyWorkAvailableAsync(
                new WorkNotification 
                { 
                    Source = WorkNotification.WorkSource.GlobalQueue,
                    Priority = count // Use count as priority hint
                });
        }
    }
    
    public void Dispose()
    {
        _batchTimer?.Dispose();
    }
}
```

## Configuration
Allow tuning of worker behavior:

```csharp
public class WorkerThreadOptions
{
    public int MaxSpuriousWakeups { get; set; } = 10;
    public int NotificationBatchWindowMicroseconds { get; set; } = 100;
    public bool EnableWorkStealing { get; set; } = true;
    public int WorkStealingThreshold { get; set; } = 2;
}
```

## Testing Strategy
1. Verify no deadlocks under high concurrency
2. Test with various test counts and parallelism levels
3. Measure CPU usage reduction
4. Ensure all tests complete correctly

## Success Metrics
- 90%+ reduction in idle CPU usage
- < 1ms average notification latency
- Zero polling overhead
- Improved test throughput

## Risks and Mitigations
- **Risk**: Deadlock from missed notifications
  - **Mitigation**: Timeout-based fallback
- **Risk**: Thundering herd on notifications
  - **Mitigation**: Work batching and queue priorities
- **Risk**: Complex synchronization bugs
  - **Mitigation**: Extensive stress testing

## AOT Compatibility Notes
- Standard synchronization primitives
- No dynamic delegate creation
- Channel<T> is AOT-compatible
- All types known at compile time

## Performance Considerations
- SemaphoreSlim is highly optimized
- Channel<T> provides excellent throughput
- Work stealing amortizes contention
- NUMA awareness for large systems

## Next Steps
After implementation:
1. Profile CPU usage improvements
2. Measure test execution latency
3. Tune notification batching
4. Move to Phase 6: Event Receiver Optimization