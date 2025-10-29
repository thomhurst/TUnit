using System.Linq;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
using TUnit.Core.Tracking;
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

    public async Task ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        await _executionGuard.TryStartExecutionAsync(test.TestId,
            () => ExecuteTestInternalAsync(test, cancellationToken));
    }

    private async Task ExecuteTestInternalAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        try
        {
            await _stateManager.MarkRunningAsync(test);
            await _messageBus.InProgress(test.Context);

            _contextRestorer.RestoreContext(test);

            // Clear Result and timing from any previous execution (important for repeated tests)
            test.Context.Result = null;
            test.Context.TestStart = null;
            test.Context.TestEnd = null;

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
            await _testExecutor.EnsureTestSessionHooksExecutedAsync();

            // Execute test with retry logic - each retry gets a fresh instance
            await RetryHelper.ExecuteWithRetry(test.Context, async () =>
            {
                test.Context.TestDetails.ClassInstance = await test.CreateInstanceAsync();

                // Invalidate cached eligible event objects since ClassInstance changed
                test.Context.CachedEligibleEventObjects = null;

                // Check if this test should be skipped (after creating instance)
                if (test.Context.TestDetails.ClassInstance is SkippedTestInstance ||
                    !string.IsNullOrEmpty(test.Context.SkipReason))
                {
                    await _stateManager.MarkSkippedAsync(test, test.Context.SkipReason ?? "Test was skipped");

                    await _eventReceiverOrchestrator.InvokeTestSkippedEventReceiversAsync(test.Context, cancellationToken);

                    await _eventReceiverOrchestrator.InvokeTestEndEventReceiversAsync(test.Context, cancellationToken);

                    return;
                }

                try
                {
                    await _testInitializer.InitializeTest(test, cancellationToken);
                    test.Context.RestoreExecutionContext();
                    await _testExecutor.ExecuteAsync(test, cancellationToken);
                }
                finally
                {
                    // Dispose test instance and fire OnDispose after each attempt
                    // This ensures each retry gets a fresh instance
                    if (test.Context.Events.OnDispose?.InvocationList != null)
                    {
                        foreach (var invocation in test.Context.Events.OnDispose.InvocationList)
                        {
                            try
                            {
                                await invocation.InvokeAsync(test.Context, test.Context);
                            }
                            catch (Exception disposeEx)
                            {
                                await _logger.LogErrorAsync($"Error during OnDispose for {test.TestId}: {disposeEx}");
                            }
                        }
                    }

                    try
                    {
                        await TestExecutor.DisposeTestInstance(test);
                    }
                    catch (Exception disposeEx)
                    {
                        await _logger.LogErrorAsync($"Error disposing test instance for {test.TestId}: {disposeEx}");
                    }
                }
            });

            await _stateManager.MarkCompletedAsync(test);

        }
        catch (SkipTestException ex)
        {
            test.Context.SkipReason = ex.Message;
            await _stateManager.MarkSkippedAsync(test, ex.Message);

            await _eventReceiverOrchestrator.InvokeTestSkippedEventReceiversAsync(test.Context, cancellationToken);
        }
        catch (Exception ex)
        {
            await _stateManager.MarkFailedAsync(test, ex);
        }
        finally
        {
            var cleanupExceptions = new List<Exception>();

            await _objectTracker.UntrackObjects(test.Context, cleanupExceptions);

            var testClass = test.Metadata.TestClassType;
            var testAssembly = testClass.Assembly;
            var hookExceptions = await _testExecutor.ExecuteAfterClassAssemblyHooks(test, testClass, testAssembly, CancellationToken.None);

            if (hookExceptions.Count > 0)
            {
                foreach (var ex in hookExceptions)
                {
                    await _logger.LogErrorAsync($"Error executing After hooks for {test.TestId}: {ex}");
                }
                cleanupExceptions.AddRange(hookExceptions);
            }

            // Invoke Last event receivers for class and assembly
            try
            {
                await _eventReceiverOrchestrator.InvokeLastTestInClassEventReceiversAsync(
                    test.Context,
                    test.Context.ClassContext,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in class event receiver for {test.TestId}: {ex}");
                cleanupExceptions.Add(ex);
            }

            try
            {
                await _eventReceiverOrchestrator.InvokeLastTestInAssemblyEventReceiversAsync(
                    test.Context,
                    test.Context.ClassContext.AssemblyContext,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in assembly event receiver for {test.TestId}: {ex}");
                cleanupExceptions.Add(ex);
            }

            try
            {
                await _eventReceiverOrchestrator.InvokeLastTestInSessionEventReceiversAsync(
                    test.Context,
                    test.Context.ClassContext.AssemblyContext.TestSessionContext,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error in last test in session event receiver for {test.TestId}: {ex}");
                cleanupExceptions.Add(ex);
            }

            // If any cleanup exceptions occurred, mark the test as failed
            if (cleanupExceptions.Count > 0)
            {
                var aggregatedException = cleanupExceptions.Count == 1
                    ? cleanupExceptions[0]
                    : new AggregateException("One or more errors occurred during test cleanup", cleanupExceptions);

                await _stateManager.MarkFailedAsync(test, aggregatedException);
            }

            switch (test.State)
            {
                case TestState.NotStarted:
                case TestState.WaitingForDependencies:
                case TestState.Queued:
                case TestState.Running:
                    // This shouldn't happen
                    await _messageBus.Cancelled(test.Context, test.StartTime.GetValueOrDefault());
                    break;
                case TestState.Passed:
                    await _messageBus.Passed(test.Context, test.StartTime.GetValueOrDefault());
                    break;
                case TestState.Timeout:
                case TestState.Failed:
                    await _messageBus.Failed(test.Context, test.Context.Result?.Exception!, test.StartTime.GetValueOrDefault());
                    break;
                case TestState.Skipped:
                    await _messageBus.Skipped(test.Context, test.Context.SkipReason ?? "Skipped");
                    break;
                case TestState.Cancelled:
                    await _messageBus.Cancelled(test.Context, test.StartTime.GetValueOrDefault());
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
            if (collected.Add(dependency.Test.Context.TestDetails))
            {
                CollectAllDependencies(dependency.Test, collected, visited);
            }
        }
    }
}
