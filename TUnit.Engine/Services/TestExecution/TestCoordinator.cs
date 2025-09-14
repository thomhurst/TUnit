using TUnit.Core;
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
    private readonly TestMessagePublisher _messagePublisher;
    private readonly TestContextRestorer _contextRestorer;
    private readonly TestExecutor _testExecutor;
    private readonly TestInitializer _testInitializer;
    private readonly TUnitFrameworkLogger _logger;

    public TestCoordinator(
        TestExecutionGuard executionGuard,
        TestStateManager stateManager,
        TestMessagePublisher messagePublisher,
        TestContextRestorer contextRestorer,
        TestExecutor testExecutor,
        TestInitializer testInitializer,
        TUnitFrameworkLogger logger)
    {
        _executionGuard = executionGuard;
        _stateManager = stateManager;
        _messagePublisher = messagePublisher;
        _contextRestorer = contextRestorer;
        _testExecutor = testExecutor;
        _testInitializer = testInitializer;
        _logger = logger;
    }

    public async Task ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        await _executionGuard.TryStartExecutionAsync(test.TestId,
            () => ExecuteTestInternalAsync(test, cancellationToken)).ConfigureAwait(false);
    }

    private async Task ExecuteTestInternalAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        try
        {
            await _stateManager.MarkRunningAsync(test).ConfigureAwait(false);
            await _messagePublisher.PublishStartedAsync(test).ConfigureAwait(false);

            _contextRestorer.RestoreContext(test);

            TestContext.Current = test.Context;
            test.Context.TestDetails.ClassInstance = await test.CreateInstanceAsync();

            // Check if this test should be skipped (after creating instance)
            if (test.Context.TestDetails.ClassInstance is SkippedTestInstance ||
                !string.IsNullOrEmpty(test.Context.SkipReason))
            {
                await _stateManager.MarkSkippedAsync(test, test.Context.SkipReason ?? "Test was skipped").ConfigureAwait(false);
                await _messagePublisher.PublishSkippedAsync(test, test.Context.SkipReason ?? "Test was skipped").ConfigureAwait(false);
                return;
            }

            await _testInitializer.InitializeTest(test, cancellationToken).ConfigureAwait(false);

            test.Context.RestoreExecutionContext();

            await _testExecutor.ExecuteAsync(test, cancellationToken).ConfigureAwait(false);

            await _stateManager.MarkCompletedAsync(test).ConfigureAwait(false);
            await _messagePublisher.PublishCompletedAsync(test).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _stateManager.MarkFailedAsync(test, ex).ConfigureAwait(false);
            await _messagePublisher.PublishFailedAsync(test, ex).ConfigureAwait(false);
        }
    }
}
