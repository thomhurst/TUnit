using TUnit.Core.Interfaces;

namespace TUnit.Core;

public abstract class GenericAbstractExecutor : IHookExecutor, ITestExecutor
{
    protected abstract ValueTask ExecuteAsync(Func<ValueTask> action);

    public ValueTask ExecuteBeforeTestDiscoveryHook(TestMethod hookMethodInfo, BeforeTestDiscoveryContext context,
        Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeTestSessionHook(TestMethod hookMethodInfo, TestSessionContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeAssemblyHook(TestMethod hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeClassHook(TestMethod hookMethodInfo, ClassHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteBeforeTestHook(TestMethod hookMethodInfo, TestContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterTestDiscoveryHook(TestMethod hookMethodInfo, TestDiscoveryContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterTestSessionHook(TestMethod hookMethodInfo, TestSessionContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterAssemblyHook(TestMethod hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterClassHook(TestMethod hookMethodInfo, ClassHookContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteAfterTestHook(TestMethod hookMethodInfo, TestContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        return ExecuteAsync(action);
    }

    public int Order => 0;
}
