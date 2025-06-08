using TUnit.Core.Interfaces;

namespace TUnit.Core.Events;

public class FirstTestInClassEventWrapper(AsyncEvent<(ClassHookContext, TestContext)>.Invocation invocation) : IFirstTestInClassEventReceiver
{
    public int Order => invocation.Order;

    public ValueTask OnFirstTestInClass(ClassHookContext context, TestContext testContext)
    {
        return invocation.InvokeAsync(sender: this, eventArgs: (context, testContext));
    }
}