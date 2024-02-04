namespace TUnit.Engine;

public class Disposer
{
    public ValueTask DisposeAsync(object? obj)
    {
        if (obj is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        if (obj is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        return ValueTask.CompletedTask;
    }
}