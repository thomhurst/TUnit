using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class DefaultExecutor : IHookExecutor, ITestExecutor
{
    public static readonly DefaultExecutor Instance = new();
    
    private DefaultExecutor()
    {
    }
    
    public Task ExecuteAssemblyHook(AssemblyHookContext context, Func<Task> action)
    {
        return Execute(action);
    }

    public Task ExecuteClassHook(ClassHookContext context, Func<Task> action)
    {
        return Execute(action);
    }

    public Task ExecuteTestHook(TestContext context, Func<Task> action)
    {
        return Execute(action);
    }

    public Task ExecuteTest(TestContext context, Func<Task> action)
    {
        return Execute(action);
    }
    
    private static Task Execute(Func<Task> action) => action();
}