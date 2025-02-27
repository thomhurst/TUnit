using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._1924;

public class DataClass : IAsyncInitializer, IAsyncDisposable
{
    public bool Disposed { get; private set; }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return default;
    }

    public ValueTask DoSomething()
    {
        if (Disposed)
        {
            throw new ObjectDisposedException(nameof(DataClass));
        }
        
        return default;
    }
}