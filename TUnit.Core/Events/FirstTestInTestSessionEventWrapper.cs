using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class FirstTestInTestSessionEventWrapper(AsyncEvent<(TestSessionContext, TestContext)>.Invocation invocation) : IFirstTestInTestSessionEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnFirstTestInTestSession(TestSessionContext context, TestContext testContext)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: (context, testContext));
    }
}
