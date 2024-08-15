using System.Reflection;

namespace TUnit.Core.Interfaces;

public interface IHookExecutor
{
    Task ExecuteBeforeTestDiscoveryHook(MethodInfo hookMethodInfo, Func<Task> action);
    Task ExecuteBeforeTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Func<Task> action);
    Task ExecuteBeforeAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Func<Task> action);
    Task ExecuteBeforeClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Func<Task> action);
    Task ExecuteBeforeTestHook(MethodInfo hookMethodInfo, TestContext context, Func<Task> action);
    
    Task ExecuteAfterTestDiscoveryHook(MethodInfo hookMethodInfo, TestDiscoveryContext context, Func<Task> action);
    Task ExecuteAfterTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Func<Task> action);
    Task ExecuteAfterAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Func<Task> action);
    Task ExecuteAfterClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Func<Task> action);
    Task ExecuteAfterTestHook(MethodInfo hookMethodInfo, TestContext context, Func<Task> action);
}