using TUnit.Core;

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
    
    public async Task InvokeTestAsync(string testMethodKey, object instance, object?[] arguments)
    {
        // Use generic delegate (boxing-free alternative to strongly-typed delegates with DynamicInvoke)
        var genericInvoker = TestDelegateStorage.GetTestInvoker(testMethodKey);
        if (genericInvoker != null)
        {
            await genericInvoker(instance, arguments);
            return;
        }
        
        // Try strongly-typed delegate as fallback (only if no generic invoker available)
        var stronglyTypedDelegate = TestDelegateStorage.GetStronglyTypedDelegate(testMethodKey);
        if (stronglyTypedDelegate != null)
        {
            // Note: DynamicInvoke creates boxing, but used as fallback for compatibility
            var result = stronglyTypedDelegate.DynamicInvoke([instance, ..arguments]);
            
            if (result is Task task)
            {
                await task;
            }
            else if (result is ValueTask valueTask)
            {
                await valueTask.AsTask();
            }
            
            return;
        }
        
        throw new InvalidOperationException(
            $"No test invoker found for {testMethodKey}. Ensure source generators have run and test is properly registered.");
    }
}
