using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Executes individual tests for the scheduler
/// Integrates with SingleTestExecutor and handles message bus communication and fail-fast logic
/// </summary>
public sealed class TestRunner : IDataProducer
{
    private readonly ITestCoordinator _testCoordinator;
    private readonly IMessageBus _messageBus;
    private readonly ITUnitMessageBus _tunitMessageBus;
    private readonly SessionUid _sessionUid;
    private readonly bool _isFailFastEnabled;
    private readonly CancellationTokenSource _failFastCancellationSource;
    private readonly TUnitFrameworkLogger _logger;
    private readonly TestStateManager _testStateManager;

    public string Uid => "TUnit.TestExecutor";
    public string Version => "1.0.0";
    public string DisplayName => "TUnit Test Executor";
    public string Description => "Executes individual tests with message bus integration and fail-fast support";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    internal TestRunner(
        ITestCoordinator testCoordinator,
        IMessageBus messageBus,
        ITUnitMessageBus tunitMessageBus,
        SessionUid sessionUid,
        bool isFailFastEnabled,
        CancellationTokenSource failFastCancellationSource,
        TUnitFrameworkLogger logger,
        TestStateManager testStateManager)
    {
        _testCoordinator = testCoordinator;
        _messageBus = messageBus;
        _tunitMessageBus = tunitMessageBus;
        _sessionUid = sessionUid;
        _isFailFastEnabled = isFailFastEnabled;
        _failFastCancellationSource = failFastCancellationSource;
        _logger = logger;
        _testStateManager = testStateManager;
    }

    private readonly GetOnlyDictionary<string, Task> _executingTests = new();
    private Exception? _firstFailFastException;

    public async Task ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        // Prevent double execution with a simple lock
        var executionTask = _executingTests.GetOrAdd(test.TestId, _ => ExecuteTestInternalAsync(test, cancellationToken));
        await executionTask.ConfigureAwait(false);
    }

    private async Task ExecuteTestInternalAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        try
        {
            // First, execute all dependencies recursively
            foreach (var dependency in test.Dependencies)
            {
                await ExecuteTestAsync(dependency.Test, cancellationToken).ConfigureAwait(false);

                if (dependency.Test.State == TestState.Failed && !dependency.ProceedOnFailure)
                {
                    await _testStateManager.MarkSkippedAsync(test, "Skipped due to failed dependencies").ConfigureAwait(false);
                    await _tunitMessageBus.Skipped(test.Context, "Skipped due to failed dependencies").ConfigureAwait(false);
                    return;
                }
            }

            test.State = TestState.Running;
            test.StartTime = DateTimeOffset.UtcNow;

            await _tunitMessageBus.InProgress(test.Context).ConfigureAwait(false);

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
            await _testStateManager.MarkFailedAsync(test, ex).ConfigureAwait(false);

            await _tunitMessageBus.Failed(test.Context, ex, test.StartTime.GetValueOrDefault()).ConfigureAwait(false);
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
