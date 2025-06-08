using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class TestInitializeEventWrapper(AsyncEvent<TestContext>.Invocation invocation) : IAsyncInitializer
{
    public int Order => invocation.Order;
    
    public Task InitializeAsync()
    {
        return invocation.InvokeAsync(sender:this, eventArgs: TestContext.Current!).AsTask();
    }
}