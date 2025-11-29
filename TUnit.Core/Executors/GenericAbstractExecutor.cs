using TUnit.Core.Interfaces;

namespace TUnit.Core;

public abstract class GenericAbstractExecutor : IHookExecutor, ITestExecutor
{
    protected abstract ValueTask ExecuteAsync(Func<ValueTask> action);

    public ValueTask ExecuteBeforeTestDiscoveryHook(MethodMetadata hookMethodInfo, BeforeTestDiscoveryContext context,
        Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeTestSessionHook(MethodMetadata hookMethodInfo, TestSessionContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeAssemblyHook(MethodMetadata hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeClassHook(MethodMetadata hookMethodInfo, ClassHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeTestHook(MethodMetadata hookMethodInfo, TestContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterTestDiscoveryHook(MethodMetadata hookMethodInfo, TestDiscoveryContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterTestSessionHook(MethodMetadata hookMethodInfo, TestSessionContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterAssemblyHook(MethodMetadata hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterClassHook(MethodMetadata hookMethodInfo, ClassHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterTestHook(MethodMetadata hookMethodInfo, TestContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public int Order => 0;
}
