using System.Collections.Concurrent;
using TUnit.Core;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Tracks test completion and manages dependency resolution
/// </summary>
public sealed class TestCompletionTracker
{
    private readonly Dictionary<string, TestExecutionState> _graph;
    private readonly ConcurrentQueue<TestExecutionState> _readyQueue;
    private readonly object _lock = new();
    private int _completedCount;
    private int _totalCount;

    public TestCompletionTracker(
        Dictionary<string, TestExecutionState> graph,
        ConcurrentQueue<TestExecutionState> readyQueue)
    {
        _graph = graph;
        _readyQueue = readyQueue;
        _totalCount = graph.Count;
    }

    public int CompletedCount => _completedCount;
    public int TotalCount => _totalCount;
    public bool AllTestsCompleted => _completedCount >= _totalCount;

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
        lock (_lock)
        {
            return _graph.Values
                .Where(s => s.State != TestState.Passed && s.State != TestState.Failed && s.State != TestState.Skipped)
                .ToList();
        }
    }
}
