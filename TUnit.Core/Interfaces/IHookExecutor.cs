namespace TUnit.Core.Interfaces;

public interface IHookExecutor
{
    ValueTask ExecuteBeforeTestDiscoveryHook(TestMethod hookMethodInfo, BeforeTestDiscoveryContext context, Func<ValueTask> action);
    ValueTask ExecuteBeforeTestSessionHook(TestMethod hookMethodInfo, TestSessionContext context, Func<ValueTask> action);
    ValueTask ExecuteBeforeAssemblyHook(TestMethod hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action);
    ValueTask ExecuteBeforeClassHook(TestMethod hookMethodInfo, ClassHookContext context, Func<ValueTask> action);
    ValueTask ExecuteBeforeTestHook(TestMethod hookMethodInfo, TestContext context, Func<ValueTask> action);
    
    ValueTask ExecuteAfterTestDiscoveryHook(TestMethod hookMethodInfo, TestDiscoveryContext context, Func<ValueTask> action);
    ValueTask ExecuteAfterTestSessionHook(TestMethod hookMethodInfo, TestSessionContext context, Func<ValueTask> action);
    ValueTask ExecuteAfterAssemblyHook(TestMethod hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action);
    ValueTask ExecuteAfterClassHook(TestMethod hookMethodInfo, ClassHookContext context, Func<ValueTask> action);
    ValueTask ExecuteAfterTestHook(TestMethod hookMethodInfo, TestContext context, Func<ValueTask> action);
}