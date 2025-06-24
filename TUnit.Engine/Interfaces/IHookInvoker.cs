using System;
using System.Reflection;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Interface for invoking hook methods
/// </summary>
public interface IHookInvoker
{
    Task InvokeHook(HookMetadata hook, HookContext context);
    Task InvokeHookAsync(object? instance, MethodInfo method, HookContext context);
    Task InvokeHookAsync(object? instance, Func<object?, HookContext, Task> hookInvoker, HookContext context);
}