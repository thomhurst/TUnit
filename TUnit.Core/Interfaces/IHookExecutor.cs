namespace TUnit.Core.Interfaces;

public interface IHookExecutor
{
    Task ExecuteAssemblyHook(AssemblyHookContext context, Func<Task> action);
    Task ExecuteClassHook(ClassHookContext context, Func<Task> action);
    Task ExecuteTestHook(TestContext context, Func<Task> action);
}