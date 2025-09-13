using TUnit.Core;
using TUnit.Engine.Services;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Publishes test state changes to the message bus.
/// Single Responsibility: Message bus communication.
/// </summary>
internal sealed class TestMessagePublisher
{
    private readonly ITUnitMessageBus _messageBus;

    public TestMessagePublisher(ITUnitMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public async Task PublishStartedAsync(AbstractExecutableTest test)
    {
        await _messageBus.InProgress(test.Context).ConfigureAwait(false);
    }

    public async Task PublishCompletedAsync(AbstractExecutableTest test)
    {
        if (test.Result?.State == TestState.Passed)
        {
            await _messageBus.Passed(test.Context, test.StartTime.GetValueOrDefault()).ConfigureAwait(false);
        }
    }

    public async Task PublishFailedAsync(AbstractExecutableTest test, Exception exception)
    {
        await _messageBus.Failed(test.Context, exception, test.StartTime.GetValueOrDefault()).ConfigureAwait(false);
    }

    public async Task PublishSkippedAsync(AbstractExecutableTest test, string reason)
    {
        await _messageBus.Skipped(test.Context, reason).ConfigureAwait(false);
    }
}