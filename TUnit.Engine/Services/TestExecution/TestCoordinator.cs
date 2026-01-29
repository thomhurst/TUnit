using System.Linq;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
using TUnit.Core.Tracking;
using TUnit.Engine.Helpers;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Coordinates test execution by orchestrating focused services.
/// Single Responsibility: Test execution orchestration.
/// </summary>
internal sealed class TestCoordinator : ITestCoordinator
{
    private readonly TestExecutionGuard _executionGuard;
    private readonly TestStateManager _stateManager;
    private readonly ITUnitMessageBus _messageBus;
    private readonly TestContextRestorer _contextRestorer;
    private readonly TestExecutor _testExecutor;
    private readonly TestInitializer _testInitializer;
    private readonly ObjectTracker _objectTracker;
    private readonly TUnitFrameworkLogger _logger;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;

    public TestCoordinator(
        TestExecutionGuard executionGuard,
        TestStateManager stateManager,
        ITUnitMessageBus messageBus,
        TestContextRestorer contextRestorer,
        TestExecutor testExecutor,
        TestInitializer testInitializer,
        ObjectTracker objectTracker,
        TUnitFrameworkLogger logger,
        EventReceiverOrchestrator eventReceiverOrchestrator)
    {
        _executionGuard = executionGuard;
        _stateManager = stateManager;
        _messageBus = messageBus;
        _contextRestorer = contextRestorer;
        _testExecutor = testExecutor;
        _testInitializer = testInitializer;
        _objectTracker = objectTracker;
        _logger = logger;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
    }

    public async ValueTask ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        await _executionGuard.TryStartExecutionAsync(test.TestId,
            () => ExecuteTestInternalAsync(test, cancellationToken));
    }

    private async ValueTask ExecuteTestInternalAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        try
        {
            _stateManager.MarkRunning(test);
            // Fire-and-forget InProgress - it's informational and doesn't need to block test execution
            _ = _messageBus.InProgress(test.Context);

            _contextRestorer.RestoreContext(test);

            // Check if test was already marked as failed during registration (e.g., property injection failure)
            // If so, skip execution and report the failure immediately
            var existingResult = test.Context.Execution.Result;
            if (existingResult?.State == TestState.Failed)
            {
                var exception = existingResult.Exception ?? new InvalidOperationException("Test failed during registration");
                _stateManager.MarkFailed(test, exception);
                await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(test.Context, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Clear Result and timing from any previous execution (important for repeated tests)
            test.Context.Execution.Result = null;
            test.Context.TestStart = null;
            test.Context.Execution.TestEnd = null;

            TestContext.Current = test.Context;

            // Note: test.Context._dependencies is already populated during discovery
            // in TestBuilder.InvokePostResolutionEventsAsync after dependencies are resolved

            // Ensure TestSession hooks run before creating test instances
            await _testExecutor.EnsureTestSessionHooksExecutedAsync(cancellationToken).ConfigureAwait(false);

            // Check if we can use the fast path (no retry, no timeout)
            // Note: retryLimit == 0 means "no retries" (run once), not "unlimited retries"
            var retryLimit = test.Context.Metadata.TestDetails.RetryLimit;
            var testTimeout = test.Context.Metadata.TestDetails.Timeout;

            if (retryLimit == 0 && !testTimeout.HasValue)
            {
                // Fast path: direct execution without wrapper overhead
                test.Context.CurrentRetryAttempt = 0;
                await ExecuteTestLifecycleAsync(test, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Slow path: use retry and timeout wrappers
                await RetryHelper.ExecuteWithRetry(test.Context, async () =>
                {
                    var timeoutMessage = testTimeout.HasValue
                        ? $"Test '{test.Context.Metadata.TestDetails.TestName}' timed out after {testTimeout.Value}"
                        : null;

                    await TimeoutHelper.ExecuteWithTimeoutAsync(
                        ct => ExecuteTestLifecycleAsync(test, ct).AsTask(),
                        testTimeout,
                        cancellationToken,
                        timeoutMessage).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }

            _stateManager.MarkCompleted(test);

        }
        catch (SkipTestException ex)
        {
            test.Context.SkipReason = ex.Message;
            _stateManager.MarkSkipped(test, ex.Message);

            await _eventReceiverOrchestrator.InvokeTestSkippedEventReceiversAsync(test.Context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _stateManager.MarkFailed(test, ex);
        }
        finally
        {
            List<Exception>? cleanupExceptions = null;

            // Flush console interceptors to ensure all buffered output is captured
            // This is critical for output from Console.Write() without newline
            try
            {
                await Console.Out.FlushAsync().ConfigureAwait(false);
                await Console.Error.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception flushEx)
            {
                await _logger.LogErrorAsync($"Error flushing console output for {test.TestId}: {flushEx}").ConfigureAwait(false);
            }

            await _objectTracker.UntrackObjects(test.Context, cleanupExceptions ??= []).ConfigureAwait(false);

            var testClass = test.Metadata.TestClassType;
            var testAssembly = testClass.Assembly;
            var hookExceptions = await _testExecutor.ExecuteAfterClassAssemblyHooks(test, testClass, testAssembly, CancellationToken.None).ConfigureAwait(false);

            if (hookExceptions.Count > 0)
            {
                foreach (var ex in hookExceptions)
                {
                    await _logger.LogErrorAsync($"Error executing After hooks for {test.TestId}: {ex}").ConfigureAwait(false);
                }
                (cleanupExceptions ??= []).AddRange(hookExceptions);
            }

            // Invoke Last event receivers for class and assembly
            try
            {
                await _eventReceiverOrchestrator.InvokeLastTestInClassEventReceiversAsync(
                    test.Context,
                    test.Context.ClassContext,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in class event receiver for {test.TestId}: {ex}").ConfigureAwait(false);
                (cleanupExceptions ??= []).Add(ex);
            }

            try
            {
                await _eventReceiverOrchestrator.InvokeLastTestInAssemblyEventReceiversAsync(
                    test.Context,
                    test.Context.ClassContext.AssemblyContext,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in assembly event receiver for {test.TestId}: {ex}").ConfigureAwait(false);
                (cleanupExceptions ??= []).Add(ex);
            }

            try
            {
                await _eventReceiverOrchestrator.InvokeLastTestInSessionEventReceiversAsync(
                    test.Context,
                    test.Context.ClassContext.AssemblyContext.TestSessionContext,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in session event receiver for {test.TestId}: {ex}").ConfigureAwait(false);
                (cleanupExceptions ??= []).Add(ex);
            }

            // If any cleanup exceptions occurred, mark the test as failed
            if (cleanupExceptions is { Count: > 0 })
            {
                var aggregatedException = cleanupExceptions.Count == 1
                    ? cleanupExceptions[0]
                    : new AggregateException("One or more errors occurred during test cleanup", cleanupExceptions);

                _stateManager.MarkFailed(test, aggregatedException);
            }

            switch (test.State)
            {
                case TestState.NotStarted:
                case TestState.WaitingForDependencies:
                case TestState.Queued:
                case TestState.Running:
                    // This shouldn't happen
                    await _messageBus.Cancelled(test.Context, test.StartTime.GetValueOrDefault()).ConfigureAwait(false);
                    break;
                case TestState.Passed:
                    await _messageBus.Passed(test.Context, test.StartTime.GetValueOrDefault()).ConfigureAwait(false);
                    break;
                case TestState.Timeout:
                case TestState.Failed:
                    await _messageBus.Failed(test.Context, test.Context.Execution.Result?.Exception!, test.StartTime.GetValueOrDefault()).ConfigureAwait(false);
                    break;
                case TestState.Skipped:
                    var skipReason = test.Context.SkipReason
                                     ?? (test.Context.Execution.Result?.IsOverridden == true ? test.Context.Execution.Result.OverrideReason : null)
                                     ?? "Skipped";
                    await _messageBus.Skipped(test.Context, skipReason).ConfigureAwait(false);
                    break;
                case TestState.Cancelled:
                    await _messageBus.Cancelled(test.Context, test.StartTime.GetValueOrDefault()).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }
    }

    /// <summary>
    /// Core test lifecycle execution: instance creation, initialization, execution, and disposal.
    /// Extracted to allow bypassing retry/timeout wrappers when not needed.
    /// </summary>
    private async ValueTask ExecuteTestLifecycleAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        test.Context.Metadata.TestDetails.ClassInstance = await test.CreateInstanceAsync().ConfigureAwait(false);

        // Invalidate cached eligible event objects since ClassInstance changed
        test.Context.CachedEligibleEventObjects = null;

        // Check if this test should be skipped (after creating instance)
        if (test.Context.Metadata.TestDetails.ClassInstance is SkippedTestInstance ||
            !string.IsNullOrEmpty(test.Context.SkipReason))
        {
            _stateManager.MarkSkipped(test, test.Context.SkipReason ?? "Test was skipped");

            await _eventReceiverOrchestrator.InvokeTestSkippedEventReceiversAsync(test.Context, cancellationToken).ConfigureAwait(false);

            await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(test.Context, cancellationToken).ConfigureAwait(false);

            return;
        }

        try
        {
            _testInitializer.PrepareTest(test, cancellationToken);
            test.Context.RestoreExecutionContext();
            await _testExecutor.ExecuteAsync(test, _testInitializer, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Dispose test instance and fire OnDispose after each attempt
            // This ensures each retry gets a fresh instance
            var onDispose = test.Context.InternalEvents.OnDispose;
            if (onDispose?.InvocationList != null)
            {
                foreach (var invocation in onDispose.InvocationList)
                {
                    try
                    {
                        await invocation.InvokeAsync(test.Context, test.Context).ConfigureAwait(false);
                    }
                    catch (Exception disposeEx)
                    {
                        await _logger.LogErrorAsync($"Error during OnDispose for {test.TestId}: {disposeEx}").ConfigureAwait(false);
                    }
                }
            }

            try
            {
                await TestExecutor.DisposeTestInstance(test).ConfigureAwait(false);
            }
            catch (Exception disposeEx)
            {
                await _logger.LogErrorAsync($"Error disposing test instance for {test.TestId}: {disposeEx}").ConfigureAwait(false);
            }
        }
    }
}
