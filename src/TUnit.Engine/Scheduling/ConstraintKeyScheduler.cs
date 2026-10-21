using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.Engine.Scheduling;

internal sealed class ConstraintKeyScheduler : IConstraintKeyScheduler
{
    private readonly TestRunner _testRunner;
    private readonly TUnitFrameworkLogger _logger;
    private readonly HashSetPool _hashSetPool;

    // Constraint keys only exclude tests that share a key, so without this cap distinct
    // keys would all run at once (#7074). The limiter is the session-wide budget that the
    // unconstrained phase also draws from, because both phases run concurrently.
    private readonly MaxParallelismLimiter _maxParallelismLimiter;

    public ConstraintKeyScheduler(
        TestRunner testRunner,
        TUnitFrameworkLogger logger,
        HashSetPool hashSetPool,
        MaxParallelismLimiter maxParallelismLimiter)
    {
        _testRunner = testRunner;
        _logger = logger;
        _hashSetPool = hashSetPool;
        _maxParallelismLimiter = maxParallelismLimiter;
    }

    #if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    public async ValueTask ExecuteTestsWithConstraintsAsync(
        (AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, int Priority)[] tests,
        CancellationToken cancellationToken)
    {
        if (tests == null || tests.Length == 0)
        {
            return;
        }

        // Sort tests by priority. Defensive copy so we don't mutate the caller's array.
        // Pack (Priority, OriginalIndex) into a long key to preserve OrderBy's stability
        // (Array.Sort itself is not stable) while using the fast default Int64 comparer.
        // Replaces `tests.OrderBy(...).ToArray()` which allocates iterator + buffer + array.
        var sortedTests = new (AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, int Priority)[tests.Length];
        var sortKeys = new long[tests.Length];
        for (var i = 0; i < tests.Length; i++)
        {
            sortedTests[i] = tests[i];
            sortKeys[i] = ((long)tests[i].Priority << 32) | (uint)i;
        }
        Array.Sort(sortKeys, sortedTests);

        // Track which constraint keys are currently in use
        var lockedKeys = new HashSet<string>();
        var lockObject = new Lock();

        // Indexed structure for tests waiting for their constraint keys to become available
        var waitingTestIndex = new WaitingTestIndex();

        // Active test tasks
        var activeTasks = new List<Task>();

        // Process each test
        foreach (var (test, constraintKeys, priority) in sortedTests)
        {
            // Run continuations asynchronously: a releasing test signals waiters from its
            // finally block, and an inline continuation would run the next (possibly
            // synchronous) test on that thread before the remaining waiters are signalled.
            var startSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            bool canStart;
            lock (lockObject)
            {
                // Check if all constraint keys are available - manual loop avoids LINQ allocation
                canStart = true;
                var keyCount = constraintKeys.Count;
                for (var i = 0; i < keyCount; i++)
                {
                    if (lockedKeys.Contains(constraintKeys[i]))
                    {
                        canStart = false;
                        break;
                    }
                }

                if (canStart)
                {
                    // Lock all the constraint keys for this test
                    for (var i = 0; i < keyCount; i++)
                    {
                        lockedKeys.Add(constraintKeys[i]);
                    }
                }
                else
                {
                    // Add to the indexed waiting structure while still under lock
                    waitingTestIndex.Add(new WaitingTest
                    {
                        TestId = test.TestId,
                        ConstraintKeys = constraintKeys,
                        StartSignal = startSignal,
                        Priority = priority
                    });
                }
            }

            if (canStart)
            {
                // Start the test immediately
                if (_logger.IsTraceEnabled)
                    await _logger.LogTraceAsync($"Starting test {test.TestId} with constraint keys: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);
                startSignal.SetResult(true);

                // Dispatch to the thread pool: invoking directly would run a synchronous
                // test body on this loop's thread, serializing every test with distinct keys.
                // Pass CancellationToken.None so the delegate always runs: the keys are already
                // taken, and only its finally block releases them and signals waiting tests.
                var testTask = Task.Run(
                    () => ExecuteTestAndReleaseKeysAsync(test, constraintKeys, lockedKeys, lockObject, waitingTestIndex, cancellationToken),
                    CancellationToken.None);
                test.ExecutionTask = testTask;
                activeTasks.Add(testTask);
            }
            else
            {
                // Test was already added to the waiting index inside the lock above
                if (_logger.IsTraceEnabled)
                    await _logger.LogTraceAsync($"Queueing test {test.TestId} waiting for constraint keys: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);

                var testTask = WaitAndExecuteTestAsync(test, constraintKeys, startSignal, lockedKeys, lockObject, waitingTestIndex, cancellationToken);
                test.ExecutionTask = testTask;
                activeTasks.Add(testTask);
            }
        }

        // Wait for all tests to complete
        await Task.WhenAll(activeTasks).ConfigureAwait(false);
    }

    #if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task WaitAndExecuteTestAsync(
        AbstractExecutableTest test,
        IReadOnlyList<string> constraintKeys,
        TaskCompletionSource<bool> startSignal,
        HashSet<string> lockedKeys,
        Lock lockObject,
        WaitingTestIndex waitingTestIndex,
        CancellationToken cancellationToken)
    {
        // Wait for signal to start, always yielding. The scheduling loop calls this method
        // directly, and a running test can release our keys between the loop queueing us and
        // this await. Awaiting that completed signal normally would run a synchronous test
        // body on the loop's thread and stall every test after it (#7075).
#if NET8_0_OR_GREATER
        await startSignal.Task.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
#else
        await new ForceYieldingTaskAwaitable(startSignal.Task);
#endif

        if (_logger.IsTraceEnabled)
            await _logger.LogTraceAsync($"Starting previously queued test {test.TestId} with constraint keys: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);

        await ExecuteTestAndReleaseKeysAsync(test, constraintKeys, lockedKeys, lockObject, waitingTestIndex, cancellationToken).ConfigureAwait(false);
    }

    #if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteTestAndReleaseKeysAsync(
        AbstractExecutableTest test,
        IReadOnlyList<string> constraintKeys,
        HashSet<string> lockedKeys,
        Lock lockObject,
        WaitingTestIndex waitingTestIndex,
        CancellationToken cancellationToken)
    {
        try
        {
            // Take a parallelism slot only after the constraint keys are held: slot holders
            // are always running tests, never waiting on keys, so the two never deadlock.
            // Acquired before entering TestRunner so a dependency that reaches this test
            // first runs it inline instead of waiting behind a slot.
            var semaphore = _maxParallelismLimiter.Semaphore;
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _testRunner.ExecuteTestWithoutExecutionTaskAsync(test, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }
        finally
        {
            // Release the constraint keys and check if any waiting tests can now run
            var testsToStart = new List<WaitingTest>();

            // Rent a pooled HashSet to collect candidates; returned immediately after we
            // copy out the (priority-sorted) elements so we avoid allocating a fresh set
            // per constraint-key release on hot scheduling paths.
            var candidates = _hashSetPool.Rent<WaitingTest>();
            try
            {
                lock (lockObject)
                {
                    // Release all constraint keys for this test
                    foreach (var key in constraintKeys)
                    {
                        lockedKeys.Remove(key);
                    }

                    // Only examine tests that are waiting on the keys we just released (O(k) lookup)
                    waitingTestIndex.GetCandidatesForReleasedKeys(constraintKeys, candidates);

                    // Sort candidates by priority to respect ordering
                    // Use a simple list + sort rather than a SortedSet to avoid per-element allocation
                    var sortedCandidates = new List<WaitingTest>(candidates.Count);
                    sortedCandidates.AddRange(candidates);
                    sortedCandidates.Sort(static (a, b) => a.Priority.CompareTo(b.Priority));

                    foreach (var candidate in sortedCandidates)
                    {
                        // Check if all constraint keys are available for this candidate
                        var canStart = true;
                        var waitingKeyCount = candidate.ConstraintKeys.Count;
                        for (var i = 0; i < waitingKeyCount; i++)
                        {
                            if (lockedKeys.Contains(candidate.ConstraintKeys[i]))
                            {
                                canStart = false;
                                break;
                            }
                        }

                        if (canStart)
                        {
                            // Lock the keys for this test
                            for (var i = 0; i < waitingKeyCount; i++)
                            {
                                lockedKeys.Add(candidate.ConstraintKeys[i]);
                            }

                            // Remove from the index and mark for starting
                            waitingTestIndex.Remove(candidate);
                            testsToStart.Add(candidate);
                        }
                        // If can't start, leave it in the index for future key releases
                    }
                }
            }
            finally
            {
                _hashSetPool.Return(candidates);
            }

            // Log and signal tests to start outside the lock
            if (_logger.IsTraceEnabled)
                await _logger.LogTraceAsync($"Released constraint keys for test {test.TestId}: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);

            foreach (var testToStart in testsToStart)
            {
                if (_logger.IsTraceEnabled)
                    await _logger.LogTraceAsync($"Unblocking waiting test {testToStart.TestId} with constraint keys: {string.Join(", ", testToStart.ConstraintKeys)}").ConfigureAwait(false);
                testToStart.StartSignal.SetResult(true);
            }
        }
    }
}
