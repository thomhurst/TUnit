using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Interface for invoking hook methods without reflection - deprecated
/// </summary>
[Obsolete("IHookInvoker is no longer needed. Hooks are invoked directly with proper context types.")]
public interface IHookInvoker
{
    Task InvokeHook(HookMetadata hook, object context);
    Task InvokeHookAsync(string hookKey, object? instance, object context);
    Task InvokeHookAsync(object? instance, Func<object?, object, Task> hookInvoker, object context);
}
