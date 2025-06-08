namespace TUnit.Core.Events;

public class TestDisposeEventWrapper(AsyncEvent<TestContext>.Invocation invocation) : IAsyncDisposable
{
    public int Order => invocation.Order;

    public ValueTask DisposeAsync()
    {
        return invocation.InvokeAsync(sender: this, eventArgs: TestContext.Current!);
    }
}