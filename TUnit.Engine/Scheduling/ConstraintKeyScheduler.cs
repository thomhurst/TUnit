using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.Engine.Scheduling;

internal sealed class ConstraintKeyScheduler : IConstraintKeyScheduler
{
    private readonly TestRunner _testRunner;
    private readonly TUnitFrameworkLogger _logger;
    private readonly ParallelLimitLockProvider _parallelLimitLockProvider;

    public ConstraintKeyScheduler(
        TestRunner testRunner,
        TUnitFrameworkLogger logger,
        ParallelLimitLockProvider parallelLimitLockProvider)
    {
        _testRunner = testRunner;
        _logger = logger;
        _parallelLimitLockProvider = parallelLimitLockProvider;
    }

    #if NET6_0_OR_GREATER
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

        // Sort tests by priority
        var sortedTests = tests.OrderBy(static t => t.Priority).ToArray();

        // Track which constraint keys are currently in use
        var lockedKeys = new HashSet<string>();
        var lockObject = new object();

        // Indexed structure for tests waiting for their constraint keys to become available
        var waitingTestIndex = new WaitingTestIndex();

        // Active test tasks
        var activeTasks = new List<Task>();

        // Process each test
        foreach (var (test, constraintKeys, priority) in sortedTests)
        {
            var startSignal = new TaskCompletionSource<bool>();

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
                if (_logger.IsDebugEnabled)
                    await _logger.LogDebugAsync($"Starting test {test.TestId} with constraint keys: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);
                startSignal.SetResult(true);

                var testTask = ExecuteTestAndReleaseKeysAsync(test, constraintKeys, lockedKeys, lockObject, waitingTestIndex, cancellationToken);
                test.ExecutionTask = testTask;
                activeTasks.Add(testTask);
            }
            else
            {
                // Test was already added to the waiting index inside the lock above
                if (_logger.IsDebugEnabled)
                    await _logger.LogDebugAsync($"Queueing test {test.TestId} waiting for constraint keys: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);

                var testTask = WaitAndExecuteTestAsync(test, constraintKeys, startSignal, lockedKeys, lockObject, waitingTestIndex, cancellationToken);
                test.ExecutionTask = testTask;
                activeTasks.Add(testTask);
            }
        }

        // Wait for all tests to complete
        await Task.WhenAll(activeTasks).ConfigureAwait(false);
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task WaitAndExecuteTestAsync(
        AbstractExecutableTest test,
        IReadOnlyList<string> constraintKeys,
        TaskCompletionSource<bool> startSignal,
        HashSet<string> lockedKeys,
        object lockObject,
        WaitingTestIndex waitingTestIndex,
        CancellationToken cancellationToken)
    {
        // Wait for signal to start
        await startSignal.Task.ConfigureAwait(false);

        if (_logger.IsDebugEnabled)
            await _logger.LogDebugAsync($"Starting previously queued test {test.TestId} with constraint keys: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);

        await ExecuteTestAndReleaseKeysAsync(test, constraintKeys, lockedKeys, lockObject, waitingTestIndex, cancellationToken).ConfigureAwait(false);
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteTestAndReleaseKeysAsync(
        AbstractExecutableTest test,
        IReadOnlyList<string> constraintKeys,
        HashSet<string> lockedKeys,
        object lockObject,
        WaitingTestIndex waitingTestIndex,
        CancellationToken cancellationToken)
    {
        SemaphoreSlim? parallelLimiterSemaphore = null;

        try
        {
            // Two-phase acquisition: Acquire ParallelLimiter BEFORE executing
            // This ensures constrained resources are acquired before holding constraint keys
            if (test.Context.ParallelLimiter != null)
            {
                parallelLimiterSemaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter);
                await parallelLimiterSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            // Execute the test (constraint keys are already held by caller)
            await _testRunner.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Release ParallelLimiter if we acquired it
            parallelLimiterSemaphore?.Release();

            // Release the constraint keys and check if any waiting tests can now run
            var testsToStart = new List<WaitingTest>();

            lock (lockObject)
            {
                // Release all constraint keys for this test
                foreach (var key in constraintKeys)
                {
                    lockedKeys.Remove(key);
                }

                // Only examine tests that are waiting on the keys we just released (O(k) lookup)
                var candidates = waitingTestIndex.GetCandidatesForReleasedKeys(constraintKeys);

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

            // Log and signal tests to start outside the lock
            if (_logger.IsDebugEnabled)
                await _logger.LogDebugAsync($"Released constraint keys for test {test.TestId}: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);

            foreach (var testToStart in testsToStart)
            {
                if (_logger.IsDebugEnabled)
                    await _logger.LogDebugAsync($"Unblocking waiting test {testToStart.TestId} with constraint keys: {string.Join(", ", testToStart.ConstraintKeys)}").ConfigureAwait(false);
                testToStart.StartSignal.SetResult(true);
            }
        }
    }
}
