using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public abstract class GenericAbstractExecutor : IHookExecutor, ITestExecutor
{
    public Task ExecuteBeforeTestDiscoveryHook(MethodInfo hookMethodInfo, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public Task ExecuteBeforeTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public Task ExecuteBeforeAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public Task ExecuteBeforeClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public Task ExecuteBeforeTestHook(MethodInfo hookMethodInfo, TestContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public Task ExecuteAfterTestDiscoveryHook(MethodInfo hookMethodInfo, TestDiscoveryContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public Task ExecuteAfterTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public Task ExecuteAfterAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public Task ExecuteAfterClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public Task ExecuteAfterTestHook(MethodInfo hookMethodInfo, TestContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public Task ExecuteTest(TestContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    protected abstract Task ExecuteAsync(Func<Task> action);
}