namespace TUnit.Core.Interfaces;

public interface ITestRetryEventReceiver : IEventReceiver
{
    ValueTask OnTestRetry(TestContext context, int retryAttempt);
}