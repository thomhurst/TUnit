using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class TestSkippedEventWrapper(AsyncEvent<TestContext>.Invocation invocation) : ITestSkippedEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnTestSkipped(TestContext testContext)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: testContext);
    }
}