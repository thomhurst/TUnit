using System;
using System.Reflection;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Default implementation of hook invoker
/// </summary>
public class DefaultHookInvoker : IHookInvoker
{
    public async Task InvokeHook(HookMetadata hook, HookContext context)
    {
        if (hook.MethodInfo != null)
        {
            var instance = hook.IsStatic ? null : context.TestInstance;
            await InvokeHookAsync(instance, hook.MethodInfo, context);
        }
        else if (hook.Invoker != null)
        {
            var instance = hook.IsStatic ? null : context.TestInstance;
            await hook.Invoker(instance, context);
        }
    }
    
    public async Task InvokeHookAsync(object? instance, MethodInfo method, HookContext context)
    {
        var parameters = method.GetParameters();
        object?[] args;
        
        if (parameters.Length == 0)
        {
            args = Array.Empty<object>();
        }
        else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(HookContext))
        {
            args = new object[] { context };
        }
        else
        {
            throw new InvalidOperationException($"Hook method {method.Name} has invalid parameters. Expected no parameters or single HookContext parameter.");
        }
        
        var result = method.Invoke(instance, args);
        
        if (result is Task task)
        {
            await task;
        }
        else if (result is ValueTask valueTask)
        {
            await valueTask.AsTask();
        }
    }
    
    public async Task InvokeHookAsync(object? instance, Func<object?, HookContext, Task> hookInvoker, HookContext context)
    {
        await hookInvoker(instance, context);
    }
}