using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;


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

        if (test.Dependencies.Any(dep => dep.Test.State == TestState.Failed && !dep.ProceedOnFailure))
        {

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


            await _tunitMessageBus.Skipped(test.Context, "Skipped due to failed dependencies").ConfigureAwait(false);

            return;
        }


        

        test.State = TestState.Running;
        test.StartTime = DateTimeOffset.UtcNow;


        await _tunitMessageBus.InProgress(test.Context).ConfigureAwait(false);

        bool hookStarted = false;
        try
        {
            if (test.Context.TestDetails.ClassInstance is PlaceholderInstance)
            {
                var instance = await test.CreateInstanceAsync().ConfigureAwait(false);
                test.Context.TestDetails.ClassInstance = instance;
            }


            var executionContext = await _hookOrchestrator.OnTestStartingAsync(test, cancellationToken).ConfigureAwait(false);
            hookStarted = true;

#if NET

            if (executionContext != null)
            {
                ExecutionContext.Restore(executionContext);
            }
#endif


            var updateMessage = await _innerExecutor.ExecuteTestAsync(test, cancellationToken).ConfigureAwait(false);

            try
            {

                await _hookOrchestrator.OnTestCompletedAsync(test, cancellationToken).ConfigureAwait(false);
            }
            finally
            {


                await RouteTestResult(test, updateMessage).ConfigureAwait(false);
            }


            if (_isFailFastEnabled && test.Result?.State == TestState.Failed)
            {
                await _logger.LogErrorAsync($"Test {test.TestId} failed. Triggering fail-fast cancellation.").ConfigureAwait(false);
                _failFastCancellationSource.Cancel();
            }
        }
        catch (Exception ex)
        {

            if (hookStarted)
            {
                try
                {
                    await _hookOrchestrator.OnTestCompletedAsync(test, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception hookEx)
                {

                    await _logger.LogErrorAsync($"Error executing cleanup hooks for test {test.TestId}: {hookEx}").ConfigureAwait(false);
                }
            }


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

    private async Task RouteTestResult(AbstractExecutableTest test, TestNodeUpdateMessage updateMessage)
    {
        try
        {

            var testState = test.State;
            var startTime = test.StartTime.GetValueOrDefault();

            switch (testState)
            {
                case TestState.Passed:
                    await _tunitMessageBus.Passed(test.Context, startTime).ConfigureAwait(false);
                    break;

                case TestState.Failed when test.Result?.Exception != null:
                    await _tunitMessageBus.Failed(test.Context, test.Result.Exception, startTime).ConfigureAwait(false);
                    break;

                case TestState.Timeout:
                    var timeoutException = test.Result?.Exception ?? new System.TimeoutException("Test timed out");
                    await _tunitMessageBus.Failed(test.Context, timeoutException, startTime).ConfigureAwait(false);
                    break;

                case TestState.Cancelled:
                    await _tunitMessageBus.Cancelled(test.Context, startTime).ConfigureAwait(false);
                    break;

                case TestState.Skipped:
                    var skipReason = test.Result?.OverrideReason ?? test.Context.SkipReason ?? "Test skipped";
                    await _tunitMessageBus.Skipped(test.Context, skipReason).ConfigureAwait(false);
                    break;

                default:

                    await _logger.LogErrorAsync($"Unexpected test state '{testState}' for test '{test.TestId}'. Marking as failed .").ConfigureAwait(false);

                    var unexpectedStateException = new InvalidOperationException($"Test ended in unexpected state: {testState}");
                    await _tunitMessageBus.Failed(test.Context, unexpectedStateException, startTime).ConfigureAwait(false);


                    await _messageBus.PublishAsync(this, updateMessage).ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Error routing test result for test {test.TestId}: {ex}").ConfigureAwait(false);
        }
        finally
        {

        }
    }
}
