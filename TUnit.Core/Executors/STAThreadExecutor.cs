using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class STAThreadExecutor : ITestExecutor, IHookExecutor
{
    public Task ExecuteTest(TestContext context, Func<Task> action)
    {
        return Execute(action);
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

    private static async Task Execute(Func<Task> action)
    {
        var tcs = new TaskCompletionSource<object?>();
        
        var thread = new Thread(() =>
        {
            try
            {
                action().GetAwaiter().GetResult();
                tcs.SetResult(null);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });
        
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        
        await tcs.Task;
    }
}