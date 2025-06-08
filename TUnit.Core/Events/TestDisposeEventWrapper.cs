using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class TestDisposeEventWrapper(AsyncEvent<TestContext>.Invocation invocation) : IAsyncDisposable, IEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask DisposeAsync()
    {
        return invocation.InvokeAsync(sender: this, eventArgs: TestContext.Current!);
    }
}
