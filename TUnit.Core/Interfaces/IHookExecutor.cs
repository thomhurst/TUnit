using System.Reflection;

namespace TUnit.Core.Interfaces;

public interface IHookExecutor
{
    Task ExecuteAsynchronousBeforeTestDiscoveryHook(MethodInfo hookMethodInfo, BeforeTestDiscoveryContext context, Func<Task> action);
    void ExecuteSynchronousBeforeTestDiscoveryHook(MethodInfo hookMethodInfo, BeforeTestDiscoveryContext context, Action action);
    Task ExecuteAsynchronousBeforeTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Func<Task> action);
    void ExecuteSynchronousBeforeTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Action action);
    Task ExecuteAsynchronousBeforeAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Func<Task> action);
    void ExecuteSynchronousBeforeAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Action action);
    Task ExecuteAsynchronousBeforeClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Func<Task> action);
    void ExecuteSynchronousBeforeClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Action action);
    Task ExecuteAsynchronousBeforeTestHook(MethodInfo hookMethodInfo, TestContext context, Func<Task> action);
    void ExecuteSynchronousBeforeTestHook(MethodInfo hookMethodInfo, TestContext context, Action action);
    
    Task ExecuteAsynchronousAfterTestDiscoveryHook(MethodInfo hookMethodInfo, TestDiscoveryContext context, Func<Task> action);
    void ExecuteSynchronousAfterTestDiscoveryHook(MethodInfo hookMethodInfo, TestDiscoveryContext context, Action action);
    Task ExecuteAsynchronousAfterTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Func<Task> action);
    void ExecuteSynchronousAfterTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Action action);
    Task ExecuteAsynchronousAfterAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Func<Task> action);
    void ExecuteSynchronousAfterAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Action action);
    Task ExecuteAsynchronousAfterClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Func<Task> action);
    void ExecuteSynchronousAfterClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Action action);
    Task ExecuteAsynchronousAfterTestHook(MethodInfo hookMethodInfo, TestContext context, Func<Task> action);
    void ExecuteSynchronousAfterTestHook(MethodInfo hookMethodInfo, TestContext context, Action action);
}