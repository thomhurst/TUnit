using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Interface for invoking hook methods without reflection
/// </summary>
public interface IHookInvoker
{
    Task InvokeHook(HookMetadata hook, HookContext context);
    Task InvokeHookAsync(string hookKey, object? instance, HookContext context);
    Task InvokeHookAsync(object? instance, Func<object?, HookContext, Task> hookInvoker, HookContext context);
}
