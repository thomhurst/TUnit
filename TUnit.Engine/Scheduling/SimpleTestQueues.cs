using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Scheduling;

internal class SimpleTestQueues
{
    private readonly TUnitFrameworkLogger _logger;
    private readonly ConcurrentQueue<TestExecutionData> _unconstrainedQueue = new();
    private readonly Queue<TestExecutionData> _globalNotInParallelQueue = new();
    private readonly Dictionary<string, Queue<TestExecutionData>> _keyedNotInParallelQueues = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TestExecutionData>> _parallelGroupQueues = new();
    
    private readonly object _globalLock = new();
    private readonly ConcurrentDictionary<string, object> _keyedLocks = new();

    public SimpleTestQueues(TUnitFrameworkLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void EnqueueTest(TestExecutionData testData)
    {
        var constraints = testData.Constraints;
        
        
        if (constraints.Count == 0)
        {
            // Unconstrained - can run in parallel with anything
            _unconstrainedQueue.Enqueue(testData);
        }
        else if (constraints.Contains("__global_not_in_parallel__"))
        {
            // Global NotInParallel - only one can run at a time globally
            lock (_globalLock)
            {
                _globalNotInParallelQueue.Enqueue(testData);
            }
        }
        else if (constraints.Any(c => c.StartsWith("__parallel_group_")))
        {
            // ParallelGroup - tests in same group can run together
            var groupKey = constraints.First(c => c.StartsWith("__parallel_group_"));
            var queue = _parallelGroupQueues.GetOrAdd(groupKey, _ => new ConcurrentQueue<TestExecutionData>());
            queue.Enqueue(testData);
        }
        else
        {
            // Keyed NotInParallel - only one per key can run at a time
            var key = constraints.First();
            var lockObj = _keyedLocks.GetOrAdd(key, _ => new object());
            lock (lockObj)
            {
                if (!_keyedNotInParallelQueues.ContainsKey(key))
                {
                    _keyedNotInParallelQueues[key] = new Queue<TestExecutionData>();
                }
                _keyedNotInParallelQueues[key].Enqueue(testData);
            }
        }
    }

    public TestExecutionData? TryDequeueTestWithConstraints(ConcurrentDictionary<string, int> runningConstraintKeys)
    {
        // Try unconstrained tests first (highest throughput)
        if (_unconstrainedQueue.TryDequeue(out var unconstrainedTest))
        {
            return unconstrainedTest;
        }

        // Try parallel group tests (if no conflicting groups running)
        foreach (var kvp in _parallelGroupQueues.ToList())
        {
            var groupKey = kvp.Key;
            var queue = kvp.Value;
            
            // Check if any other parallel group is running
            var otherGroupRunning = runningConstraintKeys.Any(x => 
                x.Key.StartsWith("__parallel_group_") && x.Key != groupKey && x.Value > 0);
                
            if (!otherGroupRunning && queue.TryDequeue(out var groupTest))
            {
                // Atomically acquire the constraint
                runningConstraintKeys.AddOrUpdate(groupKey, 1, (k, v) => v + 1);
                return groupTest;
            }
        }

        // Try keyed NotInParallel tests - only try keys that aren't currently running
        foreach (var kvp in _keyedNotInParallelQueues.ToList())
        {
            var key = kvp.Key;
            var queue = kvp.Value;
            
            // Skip if this constraint key is currently running
            runningConstraintKeys.TryGetValue(key, out var currentCount);
            if (currentCount > 0)
            {
                continue;
            }
            
            var lockObj = _keyedLocks.GetOrAdd(key, _ => new object());
            lock (lockObj)
            {
                if (queue.Count > 0)
                {
                    // Double-check constraint availability under lock
                    runningConstraintKeys.TryGetValue(key, out var reCheckCount);
                    if (reCheckCount == 0)
                    {
                        var testData = queue.Dequeue();
                        // Atomically acquire the constraint
                        runningConstraintKeys.AddOrUpdate(key, 1, (k, v) => v + 1);
                        return testData;
                    }
                }
            }
        }

        // Try global NotInParallel tests (if global constraint not running)
        runningConstraintKeys.TryGetValue("__global_not_in_parallel__", out var globalConstraintCount);
        if (globalConstraintCount == 0)
        {
            lock (_globalLock)
            {
                if (_globalNotInParallelQueue.Count > 0)
                {
                    // Double-check constraint availability under lock
                    runningConstraintKeys.TryGetValue("__global_not_in_parallel__", out var reCheckGlobalCount);
                    if (reCheckGlobalCount == 0)
                    {
                        var testData = _globalNotInParallelQueue.Dequeue();
                        // Atomically acquire the global constraint
                        runningConstraintKeys.AddOrUpdate("__global_not_in_parallel__", 1, (k, v) => v + 1);
                        return testData;
                    }
                }
            }
        }

        return null; // No work available
    }


    public bool IsEmpty()
    {
        if (!_unconstrainedQueue.IsEmpty) return false;
        
        lock (_globalLock)
        {
            if (_globalNotInParallelQueue.Count > 0) return false;
        }
        
        if (_parallelGroupQueues.Any(kvp => !kvp.Value.IsEmpty)) return false;
        
        foreach (var kvp in _keyedNotInParallelQueues)
        {
            var lockObj = _keyedLocks.GetOrAdd(kvp.Key, _ => new object());
            lock (lockObj)
            {
                if (kvp.Value.Count > 0) return false;
            }
        }
        
        return true;
    }

    public int GetTotalQueuedCount()
    {
        var count = _unconstrainedQueue.Count;
        
        lock (_globalLock)
        {
            count += _globalNotInParallelQueue.Count;
        }
        
        count += _parallelGroupQueues.Sum(kvp => kvp.Value.Count);
        
        foreach (var kvp in _keyedNotInParallelQueues)
        {
            var lockObj = _keyedLocks.GetOrAdd(kvp.Key, _ => new object());
            lock (lockObj)
            {
                count += kvp.Value.Count;
            }
        }
        
        return count;
    }
}