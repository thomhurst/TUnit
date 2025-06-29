using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using TUnit.Core;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Adapts the existing single test executor to the new scheduler interface
/// </summary>
public sealed class TestExecutorAdapter : ITestExecutor, IDataProducer
{
    private readonly ISingleTestExecutor _singleTestExecutor;
    private readonly IMessageBus _messageBus;
    private readonly SessionUid _sessionUid;

    // IDataProducer implementation
    public string Uid => "TUnit.TestExecutorAdapter";
    public string Version => "1.0.0";
    public string DisplayName => "Test Executor Adapter";
    public string Description => "Adapts single test executor to scheduler interface";
    public Type[] DataTypesProduced => new[] { typeof(TestNodeUpdateMessage) };
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public TestExecutorAdapter(
        ISingleTestExecutor singleTestExecutor,
        IMessageBus messageBus,
        SessionUid sessionUid)
    {
        _singleTestExecutor = singleTestExecutor;
        _messageBus = messageBus;
        _sessionUid = sessionUid;
    }

    public async Task ExecuteTestAsync(ExecutableTest test, CancellationToken cancellationToken)
    {
        test.State = TestState.Running;
        test.StartTime = DateTimeOffset.UtcNow;

        // Ensure test context is initialized
        if (test.Context == null)
        {
            test.Context = new TestContext(test.TestId, test.DisplayName);
        }

        // Report test started
        await _messageBus.PublishAsync(
            this,
            new TestNodeUpdateMessage(
                _sessionUid,
                test.Context.ToTestNode().WithProperty(InProgressTestNodeStateProperty.CachedInstance)));

        try
        {
            // Execute the test
            var updateMessage = await _singleTestExecutor.ExecuteTestAsync(test, _messageBus, cancellationToken);

            // Publish the result
            await _messageBus.PublishAsync(this, updateMessage);

            test.State = TestState.Passed;
        }
        catch (Exception ex)
        {
            test.State = TestState.Failed;
            test.Result = new TestResult
            {
                Status = Core.Enums.Status.Failed,
                Start = test.StartTime,
                End = DateTimeOffset.Now,
                Duration = DateTimeOffset.Now - test.StartTime.GetValueOrDefault(),
                Exception = ex,
                ComputerName = Environment.MachineName
            };

            await _messageBus.PublishAsync(
                this,
                new TestNodeUpdateMessage(
                    _sessionUid,
                    test.Context.ToTestNode().WithProperty(new FailedTestNodeStateProperty(ex))));

            throw;
        }
        finally
        {
            test.EndTime = DateTimeOffset.UtcNow;
        }
    }
}
