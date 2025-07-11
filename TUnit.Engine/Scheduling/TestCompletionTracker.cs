using System.Collections.Concurrent;
using TUnit.Core;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Tracks test completion and manages dependency resolution
/// </summary>
internal sealed class TestCompletionTracker
{
    private readonly ConcurrentDictionary<string, TestExecutionState> _graph;
    private readonly ConcurrentQueue<TestExecutionState> _readyQueue;
    private readonly WorkNotificationSystem? _notificationSystem;
    private int _completedCount;
    private int _totalCount;

    public TestCompletionTracker(
        Dictionary<string, TestExecutionState> graph,
        ConcurrentQueue<TestExecutionState> readyQueue,
        WorkNotificationSystem? notificationSystem = null)
    {
        _graph = new ConcurrentDictionary<string, TestExecutionState>(graph);
        _readyQueue = readyQueue;
        _notificationSystem = notificationSystem;
        _totalCount = graph.Count;
    }

    public int CompletedCount => _completedCount;
    public int TotalCount => _totalCount;
    public bool AllTestsCompleted => _completedCount >= _totalCount;

    public async ValueTask OnTestCompletedAsync(TestExecutionState completedTest)
    {
        Interlocked.Increment(ref _completedCount);

        // Process dependents
        foreach (var dependentId in completedTest.Dependents)
        {
            if (_graph.TryGetValue(dependentId, out var dependentState))
            {
                var remaining = dependentState.DecrementRemainingDependencies();

                if (remaining == 0)
                {
                    // Test is ready to run
                    _readyQueue.Enqueue(dependentState);
                    
                    // Notify workers immediately
                    if (_notificationSystem != null)
                    {
                        await _notificationSystem.NotifyWorkAvailableAsync(
                            new WorkNotification 
                            { 
                                Source = WorkNotification.WorkSource.NewlyReady,
                                Priority = 1 
                            });
                    }
                }
            }
        }
    }
    
    public void OnTestCompleted(TestExecutionState completedTest)
    {
        Interlocked.Increment(ref _completedCount);

        // Process dependents
        foreach (var dependentId in completedTest.Dependents)
        {
            if (_graph.TryGetValue(dependentId, out var dependentState))
            {
                var remaining = dependentState.DecrementRemainingDependencies();

                if (remaining == 0)
                {
                    // Test is ready to run
                    _readyQueue.Enqueue(dependentState);
                }
            }
        }
    }

    public IEnumerable<TestExecutionState> GetIncompleteTests()
    {
        // ConcurrentDictionary.Values is thread-safe
        return _graph.Values
            .Where(s => s.State != TestState.Passed && s.State != TestState.Failed && s.State != TestState.Skipped)
            .ToList();
    }
}
