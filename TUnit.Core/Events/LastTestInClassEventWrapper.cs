using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class LastTestInClassEventWrapper(AsyncEvent<(ClassHookContext, TestContext)>.Invocation invocation) : ILastTestInClassEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: (context, testContext));
    }
}
