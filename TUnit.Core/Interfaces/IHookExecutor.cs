namespace TUnit.Core.Interfaces;

public interface IHookExecutor
{
    ValueTask ExecuteBeforeTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, BeforeTestDiscoveryContext context, Func<ValueTask> action);
    ValueTask ExecuteBeforeTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Func<ValueTask> action);
    ValueTask ExecuteBeforeAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action);
    ValueTask ExecuteBeforeClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Func<ValueTask> action);
    ValueTask ExecuteBeforeTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Func<ValueTask> action);
    
    ValueTask ExecuteAfterTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, TestDiscoveryContext context, Func<ValueTask> action);
    ValueTask ExecuteAfterTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Func<ValueTask> action);
    ValueTask ExecuteAfterAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action);
    ValueTask ExecuteAfterClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Func<ValueTask> action);
    ValueTask ExecuteAfterTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Func<ValueTask> action);
}