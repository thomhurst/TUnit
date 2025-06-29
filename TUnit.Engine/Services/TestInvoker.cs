using System.Reflection;

namespace TUnit.Engine;

/// <summary>
/// Implementation of test invoker
/// </summary>
public class TestInvoker : ITestInvoker
{
    public async Task InvokeTestMethod(object instance, MethodInfo method, object?[] arguments)
    {
        var result = method.Invoke(instance, arguments);
        
        if (result is Task task)
        {
            await task;
        }
        else if (result is ValueTask valueTask)
        {
            await valueTask.AsTask();
        }
    }
    
    public async Task InvokeTestAsync(object instance, MethodInfo method, object?[] arguments)
    {
        var result = method.Invoke(instance, arguments);
        
        if (result is Task task)
        {
            await task;
        }
        else if (result is ValueTask valueTask)
        {
            await valueTask.AsTask();
        }
    }
    
    public async Task InvokeTestAsync(object instance, Func<object, object?[], Task> testInvoker, object?[] arguments)
    {
        await testInvoker(instance, arguments);
    }
}