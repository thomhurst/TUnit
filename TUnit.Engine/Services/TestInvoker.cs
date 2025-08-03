using TUnit.Core;
using TUnit.Engine.Interfaces;

namespace TUnit.Engine.Services;

/// AOT-safe test invoker using strongly-typed delegates from TestMetadata
public class TestInvoker : ITestInvoker
{
    public async Task InvokeTestAsync(object instance, Func<object, object?[], Task> testInvoker, object?[] arguments)
    {
        await testInvoker(instance, arguments);
    }
    
    public async Task InvokeTypedTestAsync(object instance, Func<object, object?[], Task> testInvoker, TypedTestArguments typedArguments)
    {
        // Pass typed arguments as a single-element array
        // The generated test invoker will check if args[0] is TypedTestArguments and handle accordingly
        await testInvoker(instance, new object?[] { typedArguments });
    }
}
