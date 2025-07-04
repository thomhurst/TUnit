using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// AOT-safe implementation of hook invoker - no longer needed with direct context passing
/// This class is kept for backward compatibility but is deprecated
/// </summary>
[Obsolete("HookInvoker is no longer needed. Hooks are invoked directly with proper context types.")]
public class HookInvoker : IHookInvoker
{
    public Task InvokeHook(HookMetadata hook, object context)
    {
        throw new NotSupportedException("HookInvoker is deprecated. Use direct hook invocation with proper context types.");
    }

    public Task InvokeHookAsync(string hookKey, object? instance, object context)
    {
        throw new NotSupportedException("HookInvoker is deprecated. Use direct hook invocation with proper context types.");
    }

    public Task InvokeHookAsync(object? instance, Func<object?, object, Task> hookInvoker, object context)
    {
        throw new NotSupportedException("HookInvoker is deprecated. Use direct hook invocation with proper context types.");
    }
}
