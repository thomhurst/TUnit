namespace TUnit.Engine;

/// <summary>
/// AOT-safe implementation of test invoker using strongly-typed delegates
/// </summary>
public class TestInvoker : ITestInvoker
{
    public async Task InvokeTestAsync(object instance, Func<object, object?[], Task> testInvoker, object?[] arguments)
    {
        await testInvoker(instance, arguments);
    }
    
    public Task InvokeTestAsync(string testMethodKey, object instance, object?[] arguments)
    {
        // This method is now deprecated - all test invocation should use the delegate-based overload
        // which uses delegates embedded in TestMetadata
        throw new NotSupportedException(
            $"String-based test invocation is no longer supported. " +
            $"Test '{testMethodKey}' should be invoked using the delegate from TestMetadata.TestInvoker.");
    }
}
