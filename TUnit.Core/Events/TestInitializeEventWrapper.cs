using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class TestInitializeEventWrapper(AsyncEvent<TestContext>.Invocation invocation) : IAsyncInitializer, IEventReceiver
{
    public int Order => invocation.Order;

    public Task InitializeAsync()
    {
        var testContext = TestContext.Current!;

        return invocation.InvokeAsync(sender: this, eventArgs: testContext).AsTask();
    }
}
