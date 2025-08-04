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

    public TestExecutionData? TryDequeueTestWithConstraints(
        HashSet<string> runningConstraints, 
        ConcurrentDictionary<string, int> runningConstraintKeys, 
        object constraintLock)
    {
        lock (constraintLock)
        {
            return TryDequeueTestInternal(runningConstraints, runningConstraintKeys);
        }
    }

    private TestExecutionData? TryDequeueTestInternal(
        HashSet<string> runningConstraints, 
        ConcurrentDictionary<string, int> runningConstraintKeys)
    {
        // Try keyed NotInParallel tests (if specific key not running) - with constraint acquisition
        foreach (var kvp in _keyedNotInParallelQueues.ToList())
        {
            var key = kvp.Key;
            var queue = kvp.Value;
            
            
            if (!runningConstraints.Contains(key))
            {
                var lockObj = _keyedLocks.GetOrAdd(key, _ => new object());
                lock (lockObj)
                {
                    if (queue.Count > 0)
                    {
                        var test = queue.Peek(); // Peek first, don't dequeue yet
                        
                        // Try to acquire constraints
                        var constraints = test.Constraints;
                        var canAcquire = true;
                        var acquired = new List<string>();
                        
                        try
                        {
                            foreach (var constraintKey in constraints)
                            {
                                runningConstraintKeys.TryGetValue(constraintKey, out var currentCount);
                                var isNotInParallelConstraint = test.State?.Constraint is NotInParallelConstraint;
                                
                                if (isNotInParallelConstraint && currentCount > 0)
                                {
                                    canAcquire = false;
                                    break;
                                }
                                
                                if (constraintKey.StartsWith("__parallel_group_"))
                                {
                                    var otherGroups = runningConstraintKeys
                                        .Where(kvp => kvp.Key.StartsWith("__parallel_group_") && kvp.Key != constraintKey && kvp.Value > 0)
                                        .Any();
                                        
                                    if (otherGroups)
                                    {
                                        canAcquire = false;
                                        break;
                                    }
                                }
                            }
                            
                            if (canAcquire)
                            {
                                // Acquire all constraints
                                foreach (var constraintKey in constraints)
                                {
                                    runningConstraintKeys.AddOrUpdate(constraintKey, 1, (k, v) => v + 1);
                                    acquired.Add(constraintKey);
                                }
                                
                                // Now dequeue the test
                                var dequeuedTest = queue.Dequeue();
                                return dequeuedTest;
                            }
                        }
                        catch
                        {
                            // Rollback on exception
                            foreach (var constraintKey in acquired)
                            {
                                runningConstraintKeys.AddOrUpdate(constraintKey, 0, (k, v) => Math.Max(0, v - 1));
                            }
                            throw;
                        }
                    }
                }
            }
        }
        
        return TryDequeueTestOld(runningConstraints); // Fallback to old logic for other queue types
    }
    
    public TestExecutionData? TryDequeueTestOld(HashSet<string> runningConstraints)
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
            var otherGroupRunning = runningConstraints.Any(c => 
                c.StartsWith("__parallel_group_") && c != groupKey);
                
            if (!otherGroupRunning && queue.TryDequeue(out var groupTest))
            {
                return groupTest;
            }
        }

        // Try keyed NotInParallel tests (if specific key not running)
        foreach (var kvp in _keyedNotInParallelQueues.ToList())
        {
            var key = kvp.Key;
            var queue = kvp.Value;
            
            
            if (!runningConstraints.Contains(key))
            {
                var lockObj = _keyedLocks.GetOrAdd(key, _ => new object());
                lock (lockObj)
                {
                    if (queue.Count > 0)
                    {
                        return queue.Dequeue();
                    }
                }
            }
        }

        // Try global NotInParallel tests (if nothing else running)
        if (runningConstraints.Count == 0)
        {
            lock (_globalLock)
            {
                if (_globalNotInParallelQueue.Count > 0)
                {
                    return _globalNotInParallelQueue.Dequeue();
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