using System.Runtime.Versioning;

namespace TUnit.Core;

[SupportedOSPlatform("windows")]
public class STAThreadExecutor : GenericAbstractExecutor
{
    protected override async ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        var tcs = new TaskCompletionSource<object?>();

        var thread = new Thread(() =>
        {
            try
            {
                var valueTask = action();
                if (!valueTask.IsCompleted)
                {
                    valueTask.AsTask().GetAwaiter().GetResult();
                }
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