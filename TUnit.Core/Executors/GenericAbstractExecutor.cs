using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public abstract class GenericAbstractExecutor : IHookExecutor, ITestExecutor
{
    protected abstract Task ExecuteAsync(Func<Task> action);
    protected abstract void ExecuteSync(Action action);

    public Task ExecuteAsynchronousBeforeTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, BeforeTestDiscoveryContext context,
        Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousBeforeTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, BeforeTestDiscoveryContext context,
        Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousBeforeTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousBeforeTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousBeforeAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousBeforeAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousBeforeClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousBeforeClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousBeforeTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousBeforeTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousAfterTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, TestDiscoveryContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousAfterTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, TestDiscoveryContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousAfterTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousAfterTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousAfterAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousAfterAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousAfterClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousAfterClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteAsynchronousAfterTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }

    public void ExecuteSynchronousAfterTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Action action)
    {
        ExecuteSync(action);
    }

    public Task ExecuteTest(TestContext context, Func<Task> action)
    {
        return ExecuteAsync(action);
    }
}