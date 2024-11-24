using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public abstract class GenericAbstractExecutor : IHookExecutor, ITestExecutor
{
    protected abstract Task ExecuteAsync(Func<Task> action);
    protected abstract void ExecuteSync(Action action);

    public Task ExecuteAsynchronousBeforeTestDiscoveryHook(MethodInfo hookMethodInfo, BeforeTestDiscoveryContext context,
        Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousBeforeTestDiscoveryHook(MethodInfo hookMethodInfo, BeforeTestDiscoveryContext context,
        Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousBeforeTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousBeforeTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousBeforeAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousBeforeAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousBeforeClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousBeforeClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousBeforeTestHook(MethodInfo hookMethodInfo, TestContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousBeforeTestHook(MethodInfo hookMethodInfo, TestContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousAfterTestDiscoveryHook(MethodInfo hookMethodInfo, TestDiscoveryContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousAfterTestDiscoveryHook(MethodInfo hookMethodInfo, TestDiscoveryContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousAfterTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousAfterTestSessionHook(MethodInfo hookMethodInfo, TestSessionContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousAfterAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousAfterAssemblyHook(MethodInfo hookMethodInfo, AssemblyHookContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousAfterClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousAfterClassHook(MethodInfo hookMethodInfo, ClassHookContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousAfterTestHook(MethodInfo hookMethodInfo, TestContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousAfterTestHook(MethodInfo hookMethodInfo, TestContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteTest(TestContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }
}