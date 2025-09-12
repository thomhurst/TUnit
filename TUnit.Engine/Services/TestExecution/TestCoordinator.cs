using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.Extensions;
using TUnit.Engine.Interfaces;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Coordinates test execution by orchestrating focused services.
/// Single Responsibility: Test execution orchestration.
/// </summary>
internal sealed class TestCoordinator : ITestOrchestrator
{
    private readonly TestExecutionGuard _executionGuard;
    private readonly TestStateManager _stateManager;
    private readonly TestMessagePublisher _messagePublisher;
    private readonly TestContextRestorer _contextRestorer;
    private readonly TestMethodInvoker _methodInvoker;
    private readonly TestExecutor _testExecutor;
    private readonly TUnitFrameworkLogger _logger;
    private readonly SessionUid _sessionUid;

    public TestCoordinator(
        TestExecutionGuard executionGuard,
        TestStateManager stateManager,
        TestMessagePublisher messagePublisher,
        TestContextRestorer contextRestorer,
        TestMethodInvoker methodInvoker,
        TestExecutor testExecutor,
        TUnitFrameworkLogger logger,
        SessionUid sessionUid)
    {
        _executionGuard = executionGuard;
        _stateManager = stateManager;
        _messagePublisher = messagePublisher;
        _contextRestorer = contextRestorer;
        _methodInvoker = methodInvoker;
        _testExecutor = testExecutor;
        _logger = logger;
        _sessionUid = sessionUid;
    }

    public void SetSessionId(SessionUid sessionUid)
    {
    }

    public async Task<TestNodeUpdateMessage> ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        var wasExecuted = await _executionGuard.TryStartExecutionAsync(test.TestId,
            () => ExecuteTestInternalAsync(test, cancellationToken)).ConfigureAwait(false);

        if (!wasExecuted)
        {
            await _logger.LogDebugAsync($"Test {test.TestId} was already executed by another thread").ConfigureAwait(false);
        }

        return CreateUpdateMessage(test);
    }

    private async Task ExecuteTestInternalAsync(AbstractExecutableTest test, CancellationToken cancellationToken)
    {
        try
        {
            await _stateManager.MarkRunningAsync(test).ConfigureAwait(false);
            await _messagePublisher.PublishStartedAsync(test).ConfigureAwait(false);

            _contextRestorer.RestoreContext(test);

            await test.CreateInstanceAsync();

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

    private TestNodeUpdateMessage CreateUpdateMessage(AbstractExecutableTest test)
    {
        var testNode = test.Context.ToTestNode()
            .WithProperty(GetTestNodeState(test));

        var standardOutput = test.Context.GetStandardOutput();
        var errorOutput = test.Context.GetErrorOutput();

        if (!string.IsNullOrEmpty(standardOutput))
        {
#pragma warning disable TPEXP
            testNode = testNode.WithProperty(new StandardOutputProperty(standardOutput));
#pragma warning restore TPEXP
        }

        if (!string.IsNullOrEmpty(errorOutput))
        {
#pragma warning disable TPEXP
            testNode = testNode.WithProperty(new StandardErrorProperty(errorOutput));
#pragma warning restore TPEXP
        }

        return new TestNodeUpdateMessage(
            sessionUid: _sessionUid,
            testNode: testNode);
    }

    private IProperty GetTestNodeState(AbstractExecutableTest test)
    {
        return test.Result?.State switch
        {
            TestState.Passed => new PassedTestNodeStateProperty(),
            TestState.Failed => new FailedTestNodeStateProperty(test.Result.Exception ?? new Exception("Unknown error")),
            TestState.Skipped => new SkippedTestNodeStateProperty(test.Result.Exception?.Message ?? "Test was skipped"),
            _ => new InProgressTestNodeStateProperty()
        };
    }
}
