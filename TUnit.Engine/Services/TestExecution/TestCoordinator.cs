using System.Linq;
using TUnit.Core;
using TUnit.Core.Exceptions;
using TUnit.Core.Logging;
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
    private readonly TUnitFrameworkLogger _logger;

    public TestCoordinator(
        TestExecutionGuard executionGuard,
        TestStateManager stateManager,
        ITUnitMessageBus messageBus,
        TestContextRestorer contextRestorer,
        TestExecutor testExecutor,
        TestInitializer testInitializer,
        TUnitFrameworkLogger logger)
    {
        _executionGuard = executionGuard;
        _stateManager = stateManager;
        _messageBus = messageBus;
        _contextRestorer = contextRestorer;
        _testExecutor = testExecutor;
        _testInitializer = testInitializer;
        _logger = logger;
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
            
            var allDependencies = new HashSet<TestDetails>();
            CollectAllDependencies(test, allDependencies, new HashSet<AbstractExecutableTest>());
            
            foreach (var dependency in allDependencies)
            {
                test.Context.Dependencies.Add(dependency);
            }
            
            // Ensure TestSession hooks run before creating test instances
            await _testExecutor.EnsureTestSessionHooksExecutedAsync();

            test.Context.TestDetails.ClassInstance = await test.CreateInstanceAsync();

            // Check if this test should be skipped (after creating instance)
            if (test.Context.TestDetails.ClassInstance is SkippedTestInstance ||
                !string.IsNullOrEmpty(test.Context.SkipReason))
            {
                await _stateManager.MarkSkippedAsync(test, test.Context.SkipReason ?? "Test was skipped");
                return;
            }

            await _testInitializer.InitializeTest(test, cancellationToken);

            test.Context.RestoreExecutionContext();

            await RetryHelper.ExecuteWithRetry(test.Context, async () =>
                await _testExecutor.ExecuteAsync(test, cancellationToken)
            );

            await _stateManager.MarkCompletedAsync(test);

        }
        catch (SkipTestException ex)
        {
            await _stateManager.MarkSkippedAsync(test, ex.Message);
        }
        catch (Exception ex)
        {
            await _stateManager.MarkFailedAsync(test, ex);
        }
        finally
        {
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
