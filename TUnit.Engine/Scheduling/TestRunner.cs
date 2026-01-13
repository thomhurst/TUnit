using System.Collections.Concurrent;
using TUnit.Core;
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

    internal TestRunner(
        ITestCoordinator testCoordinator,
        ITUnitMessageBus tunitMessageBus,
        bool isFailFastEnabled,
        CancellationTokenSource failFastCancellationSource,
        TUnitFrameworkLogger logger,
        TestStateManager testStateManager)
    {
        _testCoordinator = testCoordinator;
        _tunitMessageBus = tunitMessageBus;
        _isFailFastEnabled = isFailFastEnabled;
        _failFastCancellationSource = failFastCancellationSource;
        _logger = logger;
        _testStateManager = testStateManager;
    }

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

        return ExecuteTestWithCompletionAsync(test, cancellationToken, tcs);
    }

    private async ValueTask ExecuteTestWithCompletionAsync(AbstractExecutableTest test, CancellationToken cancellationToken, TaskCompletionSource<bool> tcs)
    {
        try
        {
            await ExecuteTestInternalAsync(test, cancellationToken).ConfigureAwait(false);
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
            // First, execute all dependencies recursively
            foreach (var dependency in test.Dependencies)
            {
                await ExecuteTestAsync(dependency.Test, cancellationToken).ConfigureAwait(false);

                if (dependency.Test.State == TestState.Failed && !dependency.ProceedOnFailure)
                {
                    _testStateManager.MarkSkipped(test, "Skipped due to failed dependencies");
                    await _tunitMessageBus.Skipped(test.Context, "Skipped due to failed dependencies").ConfigureAwait(false);
                    return;
                }
            }

            test.State = TestState.Running;
            test.StartTime = DateTimeOffset.UtcNow;

            // TestCoordinator handles sending InProgress message
            await _testCoordinator.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);

            if (_isFailFastEnabled && test.Result?.State == TestState.Failed)
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
        catch (Exception ex)
        {
            // TestCoordinator already handles marking as failed and sending Failed message
            // We only need to handle fail-fast logic here
            await _logger.LogErrorAsync($"Unhandled exception in test {test.TestId}: {ex}").ConfigureAwait(false);

            if (_isFailFastEnabled)
            {
                // Capture the first failure exception before triggering cancellation
                Interlocked.CompareExchange(ref _firstFailFastException, ex, null);
                await _logger.LogErrorAsync("Unhandled exception occurred. Triggering fail-fast cancellation.").ConfigureAwait(false);
                _failFastCancellationSource.Cancel();
            }
        }
        finally
        {
            test.EndTime = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Gets the first exception that triggered fail-fast cancellation.
    /// </summary>
    public Exception? GetFirstFailFastException() => _firstFailFastException;
}
