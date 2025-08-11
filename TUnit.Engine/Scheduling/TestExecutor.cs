using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;

/// Test executor adapter with hook orchestration, fail-fast support, and class/assembly lifecycle management
internal sealed class TestExecutor : ITestExecutor, IDataProducer
{
    private readonly ISingleTestExecutor _innerExecutor;
    private readonly IMessageBus _messageBus;
    private readonly ITUnitMessageBus _tunitMessageBus;
    private readonly SessionUid _sessionUid;
    private readonly bool _isFailFastEnabled;
    private readonly CancellationTokenSource _failFastCancellationSource;
    private readonly TUnitFrameworkLogger _logger;
    private readonly HookOrchestrator _hookOrchestrator;
    private readonly ParallelLimitLockProvider _parallelLimitLockProvider;

    // IDataProducer implementation
    public string Uid => "TUnit.TestExecutor";
    public string Version => "1.0.0";
    public string DisplayName => "Hook Orchestrating Test Executor Adapter";
    public string Description => "Test executor adapter with hook orchestration and fail-fast support";
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public TestExecutor(
        ISingleTestExecutor innerExecutor,
        IMessageBus messageBus,
        ITUnitMessageBus tunitMessageBus,
        SessionUid sessionUid,
        bool isFailFastEnabled,
        CancellationTokenSource failFastCancellationSource,
        TUnitFrameworkLogger logger,
        HookOrchestrator hookOrchestrator,
        ParallelLimitLockProvider parallelLimitLockProvider)
    {
        _innerExecutor = innerExecutor;
        _messageBus = messageBus;
        _tunitMessageBus = tunitMessageBus;
        _sessionUid = sessionUid;
        _isFailFastEnabled = isFailFastEnabled;
        _failFastCancellationSource = failFastCancellationSource;
        _logger = logger;
        _hookOrchestrator = hookOrchestrator;
        _parallelLimitLockProvider = parallelLimitLockProvider;
    }

    public async Task ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        // Check if any dependencies failed without ProceedOnFailure flag
        if (test.Dependencies.Any(dep => dep.Test.State == TestState.Failed && !dep.ProceedOnFailure))
        {
            // If any dependencies have failed without ProceedOnFailure, skip this test
            test.State = TestState.Skipped;
            test.Result = new TestResult
            {
                State = TestState.Skipped,
                Start = test.StartTime,
                End = DateTimeOffset.Now,
                Duration = DateTimeOffset.Now - test.StartTime.GetValueOrDefault(),
                ComputerName = Environment.MachineName,
                Exception = new SkipTestException("Skipped due to failed dependencies")
            };

            // Report the skipped state
            await _tunitMessageBus.Skipped(test.Context, "Skipped due to failed dependencies");

            return;
        }

        // Acquire semaphore for parallel limit if configured
        SemaphoreSlim? parallelLimitSemaphore = null;
        if (test.Context.ParallelLimiter != null)
        {
            parallelLimitSemaphore = _parallelLimitLockProvider.GetLock(test.Context.ParallelLimiter);
            await parallelLimitSemaphore.WaitAsync(cancellationToken);
        }

        try
        {
            // Simple state management - scheduler ensures we only get here for executable tests
            test.State = TestState.Running;
            test.StartTime = DateTimeOffset.UtcNow;

            // Report test started
            await _tunitMessageBus.InProgress(test.Context);

            bool hookStarted = false;
            try
            {
                if (test.Context.TestDetails.ClassInstance is PlaceholderInstance)
                {
                    var instance = await test.CreateInstanceAsync();
                    test.Context.TestDetails.ClassInstance = instance;
                }

                // Execute class/assembly hooks on first test
                var executionContext = await _hookOrchestrator.OnTestStartingAsync(test, cancellationToken);
                hookStarted = true;

#if NET
            // Restore the accumulated context from all hooks to flow AsyncLocal values to the test
            if (executionContext != null)
            {
                ExecutionContext.Restore(executionContext);
            }
#endif

                // Execute the test and get the result message
                var updateMessage = await _innerExecutor.ExecuteTestAsync(test, cancellationToken);

                try
                {
                    // Execute cleanup hooks (After/AfterEvery for Test/Class/Assembly)
                    await _hookOrchestrator.OnTestCompletedAsync(test, cancellationToken);
                }
                finally
                {
                    // Route the result to the appropriate ITUnitMessageBus method
                    // This must always be called to report test results
                    await RouteTestResult(test, updateMessage);
                }

                // Check if we should trigger fail-fast
                if (_isFailFastEnabled && test.Result?.State == TestState.Failed)
                {
                    await _logger.LogErrorAsync($"Test {test.TestId} failed. Triggering fail-fast cancellation.");
                    _failFastCancellationSource.Cancel();
                }
            }
            catch (Exception ex)
            {
                // If hooks were started, we MUST call OnTestCompletedAsync to decrement counters
                if (hookStarted)
                {
                    try
                    {
                        await _hookOrchestrator.OnTestCompletedAsync(test, cancellationToken);
                    }
                    catch (Exception hookEx)
                    {
                        // Log but don't throw - we want to preserve the original exception
                        await _logger.LogErrorAsync($"Error executing cleanup hooks for test {test.TestId}: {hookEx}");
                    }
                }

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
                await _tunitMessageBus.Failed(test.Context, ex, test.StartTime.GetValueOrDefault());

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
                
                // CRITICAL: Ensure the test is marked as complete even if routing fails
                // This prevents hanging when tests are waiting on dependencies
                if (!test.Context.InternalExecutableTest._taskCompletionSource.Task.IsCompleted)
                {
                    test.Context.InternalExecutableTest._taskCompletionSource.TrySetResult();
                }
            }
        }
        finally
        {
            // Release semaphore if we acquired one
            parallelLimitSemaphore?.Release();
        }
    }

    private async Task RouteTestResult(AbstractExecutableTest test, TestNodeUpdateMessage updateMessage)
    {
        try
        {
            // Optimized: Use test state directly instead of searching through properties
            var testState = test.State;
            var startTime = test.StartTime.GetValueOrDefault();

            switch (testState)
            {
                case TestState.Passed:
                    await _tunitMessageBus.Passed(test.Context, startTime);
                    break;

                case TestState.Failed when test.Result?.Exception != null:
                    await _tunitMessageBus.Failed(test.Context, test.Result.Exception, startTime);
                    break;

                case TestState.Timeout:
                    var timeoutException = test.Result?.Exception ?? new System.TimeoutException("Test timed out");
                    await _tunitMessageBus.Failed(test.Context, timeoutException, startTime);
                    break;

                case TestState.Cancelled:
                    await _tunitMessageBus.Cancelled(test.Context, startTime);
                    break;

                case TestState.Skipped:
                    var skipReason = test.Result?.OverrideReason ?? test.Context.SkipReason ?? "Test skipped";
                    await _tunitMessageBus.Skipped(test.Context, skipReason);
                    break;

                default:
                    // Fallback: ensure TaskCompletionSource is always set to prevent hanging
                    await _logger.LogErrorAsync($"Unexpected test state '{testState}' for test '{test.TestId}'. Marking as failed .");

                    var unexpectedStateException = new InvalidOperationException($"Test ended in unexpected state: {testState}");
                    await _tunitMessageBus.Failed(test.Context, unexpectedStateException, startTime);

                    // Still publish the raw message for debugging
                    await _messageBus.PublishAsync(this, updateMessage);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't rethrow - we need to ensure the test completes
            await _logger.LogErrorAsync($"Error routing test result for test {test.TestId}: {ex}");
            
            // Try to mark as failed if we haven't already
            if (test.Context.InternalExecutableTest._taskCompletionSource.Task.IsCompleted == false)
            {
                try
                {
                    await _tunitMessageBus.Failed(test.Context, ex, test.StartTime.GetValueOrDefault());
                }
                catch
                {
                    // If even that fails, just set the completion directly
                    test.Context.InternalExecutableTest._taskCompletionSource.TrySetResult();
                }
            }
        }
        finally
        {
            // CRITICAL: Always ensure the TaskCompletionSource is set to prevent hanging
            // This is a safety net - normally the TUnitMessageBus methods should set it
            if (!test.Context.InternalExecutableTest._taskCompletionSource.Task.IsCompleted)
            {
                test.Context.InternalExecutableTest._taskCompletionSource.TrySetResult();
            }
        }
    }
}
