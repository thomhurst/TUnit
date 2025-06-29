using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class LastTestInTestSessionEventWrapper(AsyncEvent<(TestSessionContext, TestContext)>.Invocation invocation) : ILastTestInTestSessionEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnLastTestInTestSession(TestSessionContext context, TestContext testContext)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: (context, testContext));
    }
}
