using TUnit.Core.Interfaces;

namespace TUnit.Core;

public abstract class GenericAbstractExecutor : IHookExecutor, ITestExecutor
{
    protected abstract ValueTask ExecuteAsync(Func<ValueTask> action);

    public ValueTask ExecuteBeforeTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, BeforeTestDiscoveryContext context,
        Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, TestDiscoveryContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }
    
    public ValueTask ExecuteAfterTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }
}