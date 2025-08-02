using TUnit.Engine.Interfaces;

namespace TUnit.Engine.Services;

/// AOT-safe test invoker using strongly-typed delegates from TestMetadata
public class TestInvoker : ITestInvoker
{
    public async Task InvokeTestAsync(object instance, Func<object, object?[], Task> testInvoker, object?[] arguments)
    {
        await testInvoker(instance, arguments);
    }
}
