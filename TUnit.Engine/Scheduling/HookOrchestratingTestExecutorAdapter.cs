using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.Extensions;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;

/// Test executor adapter with hook orchestration, fail-fast support, and class/assembly lifecycle management
internal sealed class HookOrchestratingTestExecutorAdapter : ITestExecutor, IDataProducer
{
    private readonly ISingleTestExecutor _innerExecutor;
    private readonly IMessageBus _messageBus;
    private readonly SessionUid _sessionUid;
    private readonly bool _isFailFastEnabled;
    private readonly CancellationTokenSource _failFastCancellationSource;
    private readonly TUnitFrameworkLogger _logger;
    private readonly HookOrchestrator _hookOrchestrator;

    // IDataProducer implementation
    public string Uid => "TUnit.HookOrchestratingTestExecutorAdapter";
    public string Version => "1.0.0";
    public string DisplayName => "Hook Orchestrating Test Executor Adapter";
    public string Description => "Test executor adapter with hook orchestration and fail-fast support";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public HookOrchestratingTestExecutorAdapter(
        ISingleTestExecutor innerExecutor,
        IMessageBus messageBus,
        SessionUid sessionUid,
        bool isFailFastEnabled,
        CancellationTokenSource failFastCancellationSource,
        TUnitFrameworkLogger logger,
        HookOrchestrator hookOrchestrator)
    {
        _innerExecutor = innerExecutor;
        _messageBus = messageBus;
        _sessionUid = sessionUid;
        _isFailFastEnabled = isFailFastEnabled;
        _failFastCancellationSource = failFastCancellationSource;
        _logger = logger;
        _hookOrchestrator = hookOrchestrator;
    }

    public async Task ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        // Simple state management - scheduler ensures we only get here for executable tests
        test.State = TestState.Running;
        test.StartTime = DateTimeOffset.UtcNow;

        // Report test started
        await _messageBus.PublishAsync(
            this,
            new TestNodeUpdateMessage(
                _sessionUid,
                test.Context.ToTestNode().WithProperty(InProgressTestNodeStateProperty.CachedInstance)));
        try
        {
            // Execute class/assembly hooks on first test
            var executionContext = await _hookOrchestrator.OnTestStartingAsync(test, cancellationToken);

#if NET
            ExecutionContext.Restore(executionContext);
#endif

            // Execute the test and get the result message
            var updateMessage = await _innerExecutor.ExecuteTestAsync(test, cancellationToken);

            // Publish the result
            await _messageBus.PublishAsync(this, updateMessage);

            // Check if we should trigger fail-fast
            if (_isFailFastEnabled && test.Result?.State == TestState.Failed)
            {
                await _logger.LogErrorAsync($"Test {test.TestId} failed. Triggering fail-fast cancellation.");
                _failFastCancellationSource.Cancel();
            }
        }
        catch (Exception ex)
        {
            // Set test state
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

            // Report the failure
            await _messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(
                    _sessionUid,
                    test.Context.ToTestNode().WithProperty(new FailedTestNodeStateProperty(ex))));

            // Log the exception
            await _logger.LogErrorAsync($"Unhandled exception in test {test.TestId}: {ex}");

            // If fail-fast is enabled, cancel all remaining tests
            if (_isFailFastEnabled)
            {
                await _logger.LogErrorAsync("Unhandled exception occurred. Triggering fail-fast cancellation.");
                _failFastCancellationSource.Cancel();
            }

            // Re-throw to maintain existing behavior
            throw;
        }
        finally
        {
            test.EndTime = DateTimeOffset.UtcNow;
        }
    }
}
