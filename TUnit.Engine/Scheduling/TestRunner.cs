using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Settings;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Executes individual tests for the scheduler
/// Integrates with SingleTestExecutor and handles message bus communication and fail-fast logic
/// </summary>
public sealed class TestRunner
{
    private readonly ITestCoordinator _testCoordinator;
    private readonly ITUnitMessageBus _tunitMessageBus;
    private readonly bool _isFailFastEnabled;
    private readonly CancellationTokenSource _failFastCancellationSource;
    private readonly TUnitFrameworkLogger _logger;
    private readonly TestStateManager _testStateManager;
    private readonly ParallelLimitLockProvider _parallelLimitLockProvider;

    internal TestRunner(
        ITestCoordinator testCoordinator,
        ITUnitMessageBus tunitMessageBus,
        bool isFailFastEnabled,
        CancellationTokenSource failFastCancellationSource,
        TUnitFrameworkLogger logger,
        TestStateManager testStateManager,
        ParallelLimitLockProvider parallelLimitLockProvider)
    {
        _testCoordinator = testCoordinator;
        _tunitMessageBus = tunitMessageBus;
        _isFailFastEnabled = isFailFastEnabled;
        _failFastCancellationSource = failFastCancellationSource;
        _logger = logger;
        _testStateManager = testStateManager;
        _parallelLimitLockProvider = parallelLimitLockProvider;
    }

    // Dedup ledger for re-entrant ExecuteTestAsync calls (dependency recursion, scheduler races).
    // Entries are intentionally retained for the session: a late dependency lookup must still
    // observe the in-flight or completed TCS. Session-scoped lifetime bounds growth to O(test count).
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _executingTests = new();
    private Exception? _firstFailFastException;

    public ValueTask ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        if (_executingTests.TryGetValue(test.TestId, out var existingTcs))
        {
            return new ValueTask(existingTcs.Task);
        }
        var tcs = new TaskCompletionSource<bool>();
        existingTcs = _executingTests.GetOrAdd(test.TestId, tcs);

        if (existingTcs != tcs)
        {
            return new ValueTask(existingTcs.Task);
        }

        // Skip the extra async state machine that a wrapper method would create. Start the
        // inner ValueTask, fast-path synchronous completion into the TCS, and otherwise fall
        // back to a minimal async helper that mirrors the outcome onto the TCS without
        // allocating a Task via AsTask().
        var innerTask = ExecuteTestInternalAsync(test, cancellationToken);

        if (innerTask.IsCompletedSuccessfully)
        {
            tcs.SetResult(true);
            return default;
        }

        return WrapAsync(innerTask, tcs);
    }

    private static async ValueTask WrapAsync(ValueTask inner, TaskCompletionSource<bool> tcs)
    {
        try
        {
            await inner.ConfigureAwait(false);
            tcs.SetResult(true);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
            throw;
        }
    }

    private async ValueTask ExecuteTestInternalAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        try
        {
            // First, execute all dependencies recursively (without holding the limiter
            // semaphore — avoids deadlocks in dependency chains sharing a limiter).
            foreach (var dependency in test.Dependencies)
            {
                await ExecuteTestAsync(dependency.Test, cancellationToken).ConfigureAwait(false);

                if (dependency.Test.State != TestState.Passed && !dependency.ProceedOnFailure)
                {
                    _testStateManager.MarkSkipped(test, "Skipped due to failed dependencies");
                    await _tunitMessageBus.Skipped(test.Context, "Skipped due to failed dependencies").ConfigureAwait(false);
                    return;
                }
            }

            // Acquired here (not in the scheduler) so the limit is enforced
            // regardless of entry point — scheduler or dependency recursion.
            SemaphoreSlim? acquiredLimiter = null;
            try
            {
                if (test.Context.ParallelLimiter is { } parallelLimiter)
                {
                    var limiter = _parallelLimitLockProvider.GetLock(parallelLimiter);
                    await limiter.WaitAsync(cancellationToken).ConfigureAwait(false);
                    acquiredLimiter = limiter;
                }

                test.State = TestState.Running;
                test.StartTime = DateTimeOffset.UtcNow;

                // TestCoordinator handles sending InProgress message
                await _testCoordinator.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);

                if ((_isFailFastEnabled || TUnitSettings.Default.Execution.FailFast) && test.Result?.State == TestState.Failed)
                {
                    // Capture the first failure exception before triggering cancellation
                    if (test.Result.Exception != null)
                    {
                        Interlocked.CompareExchange(ref _firstFailFastException, test.Result.Exception, null);
                    }
                    await _logger.LogErrorAsync($"Test {test.TestId} failed. Triggering fail-fast cancellation.").ConfigureAwait(false);
                    _failFastCancellationSource.Cancel();
                }
            }
            finally
            {
                acquiredLimiter?.Release();
            }
        }
        catch (Exception ex)
        {
            // TestCoordinator already handles marking as failed and sending Failed message
            // We only need to handle fail-fast logic here
            await _logger.LogErrorAsync($"Unhandled exception in test {test.TestId}: {ex}").ConfigureAwait(false);

            if (_isFailFastEnabled || TUnitSettings.Default.Execution.FailFast)
            {
                // Capture the first failure exception before triggering cancellation
                Interlocked.CompareExchange(ref _firstFailFastException, ex, null);
                await _logger.LogErrorAsync("Unhandled exception occurred. Triggering fail-fast cancellation.").ConfigureAwait(false);
                _failFastCancellationSource.Cancel();
            }
        }
        finally
        {
            test.EndTime ??= DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Gets the first exception that triggered fail-fast cancellation.
    /// </summary>
    public Exception? GetFirstFailFastException() => _firstFailFastException;
}
