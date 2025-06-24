namespace TUnit.Core.Interfaces;

public interface ITestRetryEventReceiver : IEventReceiver
{
    ValueTask OnTestRetry(AfterTestContext context, int retryAttempt);
}