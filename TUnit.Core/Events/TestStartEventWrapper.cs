using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class TestStartEventWrapper(AsyncEvent<BeforeTestContext>.Invocation invocation) : ITestStartEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnTestStart(BeforeTestContext beforeTestContext)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: beforeTestContext);
    }
}