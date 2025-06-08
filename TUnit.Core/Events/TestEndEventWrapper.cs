using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class TestEndEventWrapper(AsyncEvent<AfterTestContext>.Invocation invocation) : ITestEndEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnTestEnd(AfterTestContext testContext)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: testContext);
    }
}