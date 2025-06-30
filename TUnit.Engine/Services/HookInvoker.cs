using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// AOT-safe implementation of hook invoker using strongly-typed delegates
/// </summary>
public class HookInvoker : IHookInvoker
{
    public async Task InvokeHook(HookMetadata hook, HookContext context)
    {
        // Use AOT-compiled delegates only
        if (hook.Invoker != null)
        {
            var instance = hook.IsStatic ? null : context.TestInstance;
            await hook.Invoker(instance, context);
        }
        else
        {
            throw new InvalidOperationException($"Hook {hook.Name} does not have a pre-compiled invoker. Ensure source generators have run.");
        }
    }

    public async Task InvokeHookAsync(string hookKey, object? instance, HookContext context)
    {
        // Try to get hook from storage
        var hookInvoker = HookDelegateStorage.GetHook(hookKey);
        if (hookInvoker != null)
        {
            await hookInvoker(instance, context);
            return;
        }
        
        throw new InvalidOperationException(
            $"No hook invoker found for {hookKey}. Ensure source generators have run and hook is properly registered.");
    }

    public async Task InvokeHookAsync(object? instance, Func<object?, HookContext, Task> hookInvoker, HookContext context)
    {
        await hookInvoker(instance, context);
    }
}
