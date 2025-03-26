namespace TUnit.Core;

public class DedicatedThreadExecutor : GenericAbstractExecutor
{
    protected sealed override async ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        var tcs = new TaskCompletionSource<object?>();

        var thread = new Thread(() =>
        {
            try
            {
                Initialize();

                var valueTask = action();
                
                if (!valueTask.IsCompletedSuccessfully)
                {
                    valueTask.AsTask().GetAwaiter().GetResult();
                }

                tcs.SetResult(null);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
            finally
            {
                CleanUp();
            }
        });

        ConfigureThread(thread);
        
        thread.Start();

        await tcs.Task;
    }

    protected virtual void ConfigureThread(Thread thread)
    {
    }
    
    protected virtual void Initialize()
    {
    }

    protected virtual void CleanUp()
    {
    }
}