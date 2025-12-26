using System.Collections.Concurrent;
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
        
        // Queue for tests waiting for their constraint keys to become available
        var waitingTests = new ConcurrentQueue<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TaskCompletionSource<bool> StartSignal)>();
        
        // Active test tasks
        var activeTasks = new List<Task>();

        // Process each test
        foreach (var (test, constraintKeys, _) in sortedTests)
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
            }

            if (canStart)
            {
                // Start the test immediately
                await _logger.LogDebugAsync($"Starting test {test.TestId} with constraint keys: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);
                startSignal.SetResult(true);
                
                var testTask = ExecuteTestAndReleaseKeysAsync(test, constraintKeys, lockedKeys, lockObject, waitingTests, cancellationToken);
                test.ExecutionTask = testTask;
                activeTasks.Add(testTask);
            }
            else
            {
                // Queue the test to wait for its keys
                await _logger.LogDebugAsync($"Queueing test {test.TestId} waiting for constraint keys: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);
                waitingTests.Enqueue((test, constraintKeys, startSignal));
                
                var testTask = WaitAndExecuteTestAsync(test, constraintKeys, startSignal, lockedKeys, lockObject, waitingTests, cancellationToken);
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
        ConcurrentQueue<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TaskCompletionSource<bool> StartSignal)> waitingTests,
        CancellationToken cancellationToken)
    {
        // Wait for signal to start
        await startSignal.Task.ConfigureAwait(false);
        
        await _logger.LogDebugAsync($"Starting previously queued test {test.TestId} with constraint keys: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);
        
        await ExecuteTestAndReleaseKeysAsync(test, constraintKeys, lockedKeys, lockObject, waitingTests, cancellationToken).ConfigureAwait(false);
    }

    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    private async Task ExecuteTestAndReleaseKeysAsync(
        AbstractExecutableTest test,
        IReadOnlyList<string> constraintKeys,
        HashSet<string> lockedKeys,
        object lockObject,
        ConcurrentQueue<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TaskCompletionSource<bool> StartSignal)> waitingTests,
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
            // Pre-allocate lists outside the lock to minimize lock duration
            var testsToStart = new List<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TaskCompletionSource<bool> StartSignal)>();
            var testsToRequeue = new List<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, TaskCompletionSource<bool> StartSignal)>();

            lock (lockObject)
            {
                // Release all constraint keys for this test
                foreach (var key in constraintKeys)
                {
                    lockedKeys.Remove(key);
                }

                // Check waiting tests to see if any can now run
                
                while (waitingTests.TryDequeue(out var waitingTest))
                {
                    // Check if all constraint keys are available for this waiting test - manual loop avoids LINQ allocation
                    var canStart = true;
                    var waitingKeyCount = waitingTest.ConstraintKeys.Count;
                    for (var i = 0; i < waitingKeyCount; i++)
                    {
                        if (lockedKeys.Contains(waitingTest.ConstraintKeys[i]))
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
                            lockedKeys.Add(waitingTest.ConstraintKeys[i]);
                        }

                        // Mark test to start after we exit the lock
                        testsToStart.Add(waitingTest);
                    }
                    else
                    {
                        // Still can't run, keep it in the queue
                        testsToRequeue.Add(waitingTest);
                    }
                }

                // Re-add tests that still can't run
                foreach (var waitingTestItem in testsToRequeue)
                {
                    waitingTests.Enqueue(waitingTestItem);
                }
            }
            
            // Log and signal tests to start outside the lock
            await _logger.LogDebugAsync($"Released constraint keys for test {test.TestId}: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);
            
            foreach (var testToStart in testsToStart)
            {
                await _logger.LogDebugAsync($"Unblocking waiting test {testToStart.Test.TestId} with constraint keys: {string.Join(", ", testToStart.ConstraintKeys)}").ConfigureAwait(false);
                testToStart.StartSignal.SetResult(true);
            }
        }
    }
}