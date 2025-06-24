using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class TestSkippedEventWrapper(AsyncEvent<TestContext>.Invocation invocation) : ITestSkippedEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnTestSkipped(TestRegisteredContext context)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: context);
    }
}