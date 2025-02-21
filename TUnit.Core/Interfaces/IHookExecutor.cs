using System.Reflection;

namespace TUnit.Core.Interfaces;

public interface IHookExecutor
{
    Task ExecuteAsynchronousBeforeTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, BeforeTestDiscoveryContext context, Func<Task> action);
    void ExecuteSynchronousBeforeTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, BeforeTestDiscoveryContext context, Action action);
    Task ExecuteAsynchronousBeforeTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Func<Task> action);
    void ExecuteSynchronousBeforeTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Action action);
    Task ExecuteAsynchronousBeforeAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Func<Task> action);
    void ExecuteSynchronousBeforeAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Action action);
    Task ExecuteAsynchronousBeforeClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Func<Task> action);
    void ExecuteSynchronousBeforeClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Action action);
    Task ExecuteAsynchronousBeforeTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Func<Task> action);
    void ExecuteSynchronousBeforeTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Action action);
    
    Task ExecuteAsynchronousAfterTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, TestDiscoveryContext context, Func<Task> action);
    void ExecuteSynchronousAfterTestDiscoveryHook(SourceGeneratedMethodInformation hookMethodInfo, TestDiscoveryContext context, Action action);
    Task ExecuteAsynchronousAfterTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Func<Task> action);
    void ExecuteSynchronousAfterTestSessionHook(SourceGeneratedMethodInformation hookMethodInfo, TestSessionContext context, Action action);
    Task ExecuteAsynchronousAfterAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Func<Task> action);
    void ExecuteSynchronousAfterAssemblyHook(SourceGeneratedMethodInformation hookMethodInfo, AssemblyHookContext context, Action action);
    Task ExecuteAsynchronousAfterClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Func<Task> action);
    void ExecuteSynchronousAfterClassHook(SourceGeneratedMethodInformation hookMethodInfo, ClassHookContext context, Action action);
    Task ExecuteAsynchronousAfterTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Func<Task> action);
    void ExecuteSynchronousAfterTestHook(SourceGeneratedMethodInformation hookMethodInfo, TestContext context, Action action);
}