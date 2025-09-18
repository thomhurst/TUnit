using System.Collections.Concurrent;
using System.Threading.Channels;
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

    public async ValueTask ExecuteTestsWithConstraintsAsync(
        (AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys, int Priority)[] tests,
        CancellationToken cancellationToken)
    {
        if (tests == null || tests.Length == 0)
        {
            return;
        }

        // Sort tests by priority
        var sortedTests = tests.OrderBy(t => t.Priority).ToArray();

        var constraintKeyUsage = new ConcurrentDictionary<string, int>();
        var readyTestsChannel = Channel.CreateUnbounded<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys)>();
        var pendingTests = new ConcurrentDictionary<AbstractExecutableTest, (IReadOnlyList<string> ConstraintKeys, int Priority)>();
        var activeTasks = new ConcurrentBag<Task>();

        var executorTask = Task.Run(async () => await ExecuteTestsFromChannelAsync(readyTestsChannel.Reader, constraintKeyUsage, pendingTests, activeTasks, cancellationToken).ConfigureAwait(false), cancellationToken);
        foreach (var (test, constraintKeys, priority) in sortedTests)
        {
            if (TryAcquireConstraints(constraintKeys, constraintKeyUsage))
            {
                await _logger.LogDebugAsync($"Starting test {test.TestId} with constraint keys: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);
                await readyTestsChannel.Writer.WriteAsync((test, constraintKeys), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _logger.LogDebugAsync($"Queueing test {test.TestId} waiting for constraint keys: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);
                pendingTests.TryAdd(test, (constraintKeys, priority));
            }
        }

        readyTestsChannel.Writer.TryComplete();
        
        await executorTask.ConfigureAwait(false);
        await Task.WhenAll(activeTasks).ConfigureAwait(false);
    }

    private bool TryAcquireConstraints(IReadOnlyList<string> constraintKeys, ConcurrentDictionary<string, int> constraintKeyUsage)
    {
        var acquiredKeys = new List<string>();
        
        foreach (var key in constraintKeys)
        {
            if (constraintKeyUsage.AddOrUpdate(key, 1, (k, currentValue) => currentValue == 0 ? 1 : currentValue) == 1)
            {
                acquiredKeys.Add(key);
            }
            else
            {
                foreach (var acquiredKey in acquiredKeys)
                {
                    constraintKeyUsage.AddOrUpdate(acquiredKey, 0, (k, v) => 0);
                }
                return false;
            }
        }
        
        return true;
    }
    
    private void ReleaseConstraints(IReadOnlyList<string> constraintKeys, ConcurrentDictionary<string, int> constraintKeyUsage)
    {
        foreach (var key in constraintKeys)
        {
            constraintKeyUsage.AddOrUpdate(key, 0, (k, v) => 0);
        }
    }

    private async Task ExecuteTestsFromChannelAsync(
        ChannelReader<(AbstractExecutableTest Test, IReadOnlyList<string> ConstraintKeys)> reader,
        ConcurrentDictionary<string, int> constraintKeyUsage,
        ConcurrentDictionary<AbstractExecutableTest, (IReadOnlyList<string> ConstraintKeys, int Priority)> pendingTests,
        ConcurrentBag<Task> activeTasks,
        CancellationToken cancellationToken)
    {
        var readerTask = ProcessTestsAsync();
        await readerTask.ConfigureAwait(false);
        await Task.WhenAll(activeTasks).ConfigureAwait(false);
        
        async Task ProcessTestsAsync()
        {
            await foreach (var (test, constraintKeys) in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                // Start test execution without Task.Run for better performance
                var testTask = ExecuteAndReleaseTestAsync(test, constraintKeys);
                test.ExecutionTask = testTask;
                activeTasks.Add(testTask);
            }
        }
        
        async Task ExecuteAndReleaseTestAsync(AbstractExecutableTest test, IReadOnlyList<string> constraintKeys)
        {
            try
            {
                await ExecuteTestWithParallelLimitAsync(test, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // Release constraints
                ReleaseConstraints(constraintKeys, constraintKeyUsage);
                await _logger.LogDebugAsync($"Released constraint keys for test {test.TestId}: {string.Join(", ", constraintKeys)}").ConfigureAwait(false);
                
                // Check if any pending tests can now run
                var readyTests = new List<(AbstractExecutableTest, IReadOnlyList<string>)>();
                
                // Get a snapshot of pending tests sorted by priority
                var sortedPending = pendingTests.OrderBy(p => p.Value.Priority).ToList();
                
                foreach (var kvp in sortedPending)
                {
                    if (TryAcquireConstraints(kvp.Value.ConstraintKeys, constraintKeyUsage))
                    {
                        readyTests.Add((kvp.Key, kvp.Value.ConstraintKeys));
                        pendingTests.TryRemove(kvp.Key, out _);
                    }
                }
                
                // Start ready tests directly
                foreach (var (readyTest, keys) in readyTests)
                {
                    await _logger.LogDebugAsync($"Unblocking waiting test {readyTest.TestId} with constraint keys: {string.Join(", ", keys)}").ConfigureAwait(false);
                    
                    // Start the test immediately
                    var readyTestTask = ExecuteAndReleaseTestAsync(readyTest, keys);
                    readyTest.ExecutionTask = readyTestTask;
                    activeTasks.Add(readyTestTask);
                }
            }
        }
    }

    private async Task ExecuteTestWithParallelLimitAsync(
        AbstractExecutableTest test,
        CancellationToken cancellationToken)
    {
        // Check if test has parallel limit constraint
        if (test.Context.ParallelLimiter != null)
        {
            var semaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter);
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _testRunner.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }
        else
        {
            await _testRunner.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);
        }
    }
}