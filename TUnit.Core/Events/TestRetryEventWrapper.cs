using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class TestRetryEventWrapper(AsyncEvent<(TestContext, int RetryAttempt)>.Invocation invocation) : ITestRetryEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnTestRetry(TestContext testContext, int retryAttempt)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: (testContext, retryAttempt));
    }
}