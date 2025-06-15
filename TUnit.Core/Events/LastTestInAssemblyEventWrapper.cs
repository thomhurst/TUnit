using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class LastTestInAssemblyEventWrapper(AsyncEvent<(AssemblyHookContext, TestContext)>.Invocation invocation) : ILastTestInAssemblyEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: (context, testContext));
    }
}