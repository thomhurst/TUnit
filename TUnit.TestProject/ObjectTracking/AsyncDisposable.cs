namespace TUnit.TestProject.ObjectTracking;

public class AsyncDisposable : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        IsDisposed = true;
    }

    public bool IsDisposed
    {
        get;
        private set;
    }
}
