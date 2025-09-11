using System.Collections.Concurrent;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Exceptions;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Executes individual tests for the scheduler
/// Integrates with SingleTestExecutor and handles message bus communication and fail-fast logic
/// </summary>
public sealed class TestRunner : IDataProducer
{
    private readonly ITestOrchestrator _testOrchestrator;
    private readonly IMessageBus _messageBus;
    private readonly ITUnitMessageBus _tunitMessageBus;
    private readonly SessionUid _sessionUid;
    private readonly bool _isFailFastEnabled;
    private readonly CancellationTokenSource _failFastCancellationSource;
    private readonly TUnitFrameworkLogger _logger;

    public string Uid => "TUnit.TestExecutor";
    public string Version => "1.0.0";
    public string DisplayName => "TUnit Test Executor";
    public string Description => "Executes individual tests with message bus integration and fail-fast support";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    internal TestRunner(
        ITestOrchestrator testOrchestrator,
        IMessageBus messageBus,
        ITUnitMessageBus tunitMessageBus,
        SessionUid sessionUid,
        bool isFailFastEnabled,
        CancellationTokenSource failFastCancellationSource,
        TUnitFrameworkLogger logger)
    {
        _testOrchestrator = testOrchestrator;
        _messageBus = messageBus;
        _tunitMessageBus = tunitMessageBus;
        _sessionUid = sessionUid;
        _isFailFastEnabled = isFailFastEnabled;
        _failFastCancellationSource = failFastCancellationSource;
        _logger = logger;
    }

    private readonly GetOnlyDictionary<string, Task> _executingTests = new();

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

                // Check if dependency failed and we should skip
                if (dependency.Test.State == TestState.Failed && !dependency.ProceedOnFailure)
                {
                    test.State = TestState.Skipped;
                    test.Result = new TestResult
                    {
                        State = TestState.Skipped,
                        Start = DateTimeOffset.UtcNow,
                        End = DateTimeOffset.UtcNow,
                        Duration = TimeSpan.Zero,
                        ComputerName = Environment.MachineName,
                        Exception = new SkipTestException("Skipped due to failed dependencies")
                    };
                    await _tunitMessageBus.Skipped(test.Context, "Skipped due to failed dependencies").ConfigureAwait(false);
                    return;
                }
            }

            // Send initial progress message
            test.State = TestState.Running;
            test.StartTime = DateTimeOffset.UtcNow;

            await _tunitMessageBus.InProgress(test.Context).ConfigureAwait(false);

            // Execute test through TestOrchestrator → TestExecutor
            var updateMessage = await _testOrchestrator.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);

            // Publish the result to the message bus
            await _messageBus.PublishAsync(this, updateMessage).ConfigureAwait(false);

            // Handle fail-fast logic
            if (_isFailFastEnabled && test.Result?.State == TestState.Failed)
            {
                await _logger.LogErrorAsync($"Test {test.TestId} failed. Triggering fail-fast cancellation.").ConfigureAwait(false);
                _failFastCancellationSource.Cancel();
            }
        }
        catch (Exception ex)
        {
            // Fallback error handling
            test.State = TestState.Failed;
            test.Result = new TestResult
            {
                State = TestState.Failed,
                Start = test.StartTime,
                End = DateTimeOffset.Now,
                Duration = DateTimeOffset.Now - test.StartTime.GetValueOrDefault(),
                Exception = ex,
                ComputerName = Environment.MachineName
            };

            await _tunitMessageBus.Failed(test.Context, ex, test.StartTime.GetValueOrDefault()).ConfigureAwait(false);
            await _logger.LogErrorAsync($"Unhandled exception in test {test.TestId}: {ex}").ConfigureAwait(false);

            if (_isFailFastEnabled)
            {
                await _logger.LogErrorAsync("Unhandled exception occurred. Triggering fail-fast cancellation.").ConfigureAwait(false);
                _failFastCancellationSource.Cancel();
            }

            throw;
        }
        finally
        {
            test.EndTime = DateTimeOffset.UtcNow;
        }
    }
}
