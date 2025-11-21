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
    private readonly HashSetPool _hashSetPool;

    public TestCoordinator(
        TestExecutionGuard executionGuard,
        TestStateManager stateManager,
        ITUnitMessageBus messageBus,
        TestContextRestorer contextRestorer,
        TestExecutor testExecutor,
        TestInitializer testInitializer,
        ObjectTracker objectTracker,
        TUnitFrameworkLogger logger,
        EventReceiverOrchestrator eventReceiverOrchestrator,
        HashSetPool hashSetPool)
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
        _hashSetPool = hashSetPool;
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
            await _stateManager.MarkRunningAsync(test).ConfigureAwait(false);
            await _messageBus.InProgress(test.Context).ConfigureAwait(false);

            _contextRestorer.RestoreContext(test);

            // Clear Result and timing from any previous execution (important for repeated tests)
            test.Context.Execution.Result = null;
            test.Context.TestStart = null;
            test.Context.Execution.TestEnd = null;

            TestContext.Current = test.Context;

            var allDependencies = _hashSetPool.Rent<TestDetails>();
            var visited = _hashSetPool.Rent<AbstractExecutableTest>();
            try
            {
                CollectAllDependencies(test, allDependencies, visited);

                foreach (var dependency in allDependencies)
                {
                    test.Context._dependencies.Add(dependency);
                }
            }
            finally
            {
                _hashSetPool.Return(allDependencies);
                _hashSetPool.Return(visited);
            }

            // Ensure TestSession hooks run before creating test instances
            await _testExecutor.EnsureTestSessionHooksExecutedAsync(cancellationToken).ConfigureAwait(false);

            // Execute test with retry logic - each retry gets a fresh instance
            // Timeout is applied per retry attempt, not across all retries
            await RetryHelper.ExecuteWithRetry(test.Context, async () =>
            {
                // Get timeout configuration for this attempt
                var testTimeout = test.Context.Metadata.TestDetails.Timeout;
                var timeoutMessage = testTimeout.HasValue
                    ? $"Test '{test.Context.Metadata.TestDetails.TestName}' timed out after {testTimeout.Value}"
                    : null;

                // Wrap entire lifecycle (instance creation, initialization, execution) with timeout
                await TimeoutHelper.ExecuteWithTimeoutAsync(
                    async ct =>
                    {
                        test.Context.Metadata.TestDetails.ClassInstance = await test.CreateInstanceAsync().ConfigureAwait(false);

                        // Invalidate cached eligible event objects since ClassInstance changed
                        test.Context.CachedEligibleEventObjects = null;

                        // Check if this test should be skipped (after creating instance)
                        if (test.Context.Metadata.TestDetails.ClassInstance is SkippedTestInstance ||
                            !string.IsNullOrEmpty(test.Context.SkipReason))
                        {
                            await _stateManager.MarkSkippedAsync(test, test.Context.SkipReason ?? "Test was skipped").ConfigureAwait(false);

                            await _eventReceiverOrchestrator.InvokeTestSkippedEventReceiversAsync(test.Context, ct).ConfigureAwait(false);

                            await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(test.Context, ct).ConfigureAwait(false);

                            return;
                        }

                        try
                        {
                            await _testInitializer.InitializeTest(test, ct).ConfigureAwait(false);
                            test.Context.RestoreExecutionContext();
                            await _testExecutor.ExecuteAsync(test, ct).ConfigureAwait(false);
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
                    },
                    testTimeout,
                    cancellationToken,
                    timeoutMessage).ConfigureAwait(false);
            }).ConfigureAwait(false);

            await _stateManager.MarkCompletedAsync(test).ConfigureAwait(false);

        }
        catch (SkipTestException ex)
        {
            test.Context.SkipReason = ex.Message;
            await _stateManager.MarkSkippedAsync(test, ex.Message).ConfigureAwait(false);

            await _eventReceiverOrchestrator.InvokeTestSkippedEventReceiversAsync(test.Context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _stateManager.MarkFailedAsync(test, ex).ConfigureAwait(false);
        }
        finally
        {
            var cleanupExceptions = new List<Exception>();

            await _objectTracker.UntrackObjects(test.Context, cleanupExceptions).ConfigureAwait(false);

            var testClass = test.Metadata.TestClassType;
            var testAssembly = testClass.Assembly;
            var hookExceptions = await _testExecutor.ExecuteAfterClassAssemblyHooks(test, testClass, testAssembly, CancellationToken.None).ConfigureAwait(false);

            if (hookExceptions.Count > 0)
            {
                foreach (var ex in hookExceptions)
                {
                    await _logger.LogErrorAsync($"Error executing After hooks for {test.TestId}: {ex}").ConfigureAwait(false);
                }
                cleanupExceptions.AddRange(hookExceptions);
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
                cleanupExceptions.Add(ex);
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
                cleanupExceptions.Add(ex);
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
                cleanupExceptions.Add(ex);
            }

            // If any cleanup exceptions occurred, mark the test as failed
            if (cleanupExceptions.Count > 0)
            {
                var aggregatedException = cleanupExceptions.Count == 1
                    ? cleanupExceptions[0]
                    : new AggregateException("One or more errors occurred during test cleanup", cleanupExceptions);

                await _stateManager.MarkFailedAsync(test, aggregatedException).ConfigureAwait(false);
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
                    await _messageBus.Skipped(test.Context, test.Context.SkipReason ?? "Skipped").ConfigureAwait(false);
                    break;
                case TestState.Cancelled:
                    await _messageBus.Cancelled(test.Context, test.StartTime.GetValueOrDefault()).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }
    }

    private void CollectAllDependencies(AbstractExecutableTest test, HashSet<TestDetails> collected, HashSet<AbstractExecutableTest> visited)
    {
        if (!visited.Add(test))
        {
            return;
        }

        foreach (var dependency in test.Dependencies)
        {
            if (collected.Add(dependency.Test.Context.Metadata.TestDetails))
            {
                CollectAllDependencies(dependency.Test, collected, visited);
            }
        }
    }
}
