namespace TUnit.Core.Interfaces;

public interface ITestRetryEventReceiver : IEventReceiver
{
    ValueTask OnTestRetry(TestContext testContext, int retryAttempt);
}