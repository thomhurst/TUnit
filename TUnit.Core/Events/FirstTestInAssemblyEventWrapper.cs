using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class FirstTestInAssemblyEventWrapper(AsyncEvent<(AssemblyHookContext, TestContext)>.Invocation invocation) : IFirstTestInAssemblyEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnFirstTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: (context, testContext));
    }
}
