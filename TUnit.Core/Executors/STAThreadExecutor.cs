using System.Runtime.Versioning;

namespace TUnit.Core;

[SupportedOSPlatform("windows")]
public class STAThreadExecutor : GenericAbstractExecutor
{
    protected override async Task ExecuteAsync(Func<Task> action)
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

    protected override void ExecuteSync(Action action)
    {
        var thread = new Thread(() => action());
        
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}