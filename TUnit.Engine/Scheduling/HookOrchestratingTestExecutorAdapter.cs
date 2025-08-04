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
        // If test is already failed (e.g., due to circular dependencies), report the failure and return
        if (test.State == TestState.Failed && test.Result != null)
        {
            await _logger.LogErrorAsync($"Test {test.TestId} is already failed with: {test.Result.Exception?.Message}");
            await _messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(
                    _sessionUid,
                    test.Context.ToTestNode().WithProperty(new FailedTestNodeStateProperty(test.Result.Exception!))));
            return;
        }
        
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

            // Execute cleanup hooks
            Exception? cleanupException = null;
            try
            {
                // Use a separate cancellation token for cleanup to ensure cleanup hooks execute
                // even when the main test execution is cancelled. Give cleanup a reasonable timeout.
                using var cleanupCts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                await _hookOrchestrator.OnTestCompletedAsync(test, cleanupCts.Token);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in cleanup hooks for test {test.TestId}: {ex}");
                cleanupException = ex;
                
                // If test passed but after hooks failed, update the test result
                if (test.State == TestState.Passed)
                {
                    test.State = TestState.Failed;
                    test.Result = new TestResult
                    {
                        State = TestState.Failed,
                        Start = test.StartTime,
                        End = test.EndTime,
                        Duration = test.EndTime.GetValueOrDefault() - test.StartTime.GetValueOrDefault(),
                        Exception = ex,
                        ComputerName = Environment.MachineName
                    };
                    
                    // Report the failure
                    await _messageBus.PublishAsync(
                        this,
                        new TestNodeUpdateMessage(
                            _sessionUid,
                            test.Context.ToTestNode().WithProperty(new FailedTestNodeStateProperty(ex))));
                }
                // If test already failed and after hooks also failed, we need to report both failures
                else if (test.State == TestState.Failed && test.Result?.Exception != null)
                {
                    var aggregateException = new AggregateException("Test and after hooks both failed", test.Result.Exception, ex);
                    test.Result = new TestResult
                    {
                        State = TestState.Failed,
                        Start = test.Result.Start,
                        End = test.EndTime,
                        Duration = test.Result.Duration,
                        Exception = aggregateException,
                        ComputerName = test.Result.ComputerName
                    };
                    
                    // Update the failure message
                    await _messageBus.PublishAsync(
                        this,
                        new TestNodeUpdateMessage(
                            _sessionUid,
                            test.Context.ToTestNode().WithProperty(new FailedTestNodeStateProperty(aggregateException))));
                }
            }
        }
    }
}
