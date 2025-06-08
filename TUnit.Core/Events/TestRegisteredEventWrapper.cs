using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class TestRegisteredEventWrapper(AsyncEvent<TestRegisteredContext>.Invocation invocation) : ITestRegisteredEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: context);
    }
}
