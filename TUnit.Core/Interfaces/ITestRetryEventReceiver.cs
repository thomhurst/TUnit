namespace TUnit.Core.Interfaces;

public interface ITestRetryEventReceiver : IEventReceiver
{
    Task OnTestRetry(TestContext testContext, int retryAttempt);
}