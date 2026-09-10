using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Models;

public record InitialisableClass : IAsyncInitializer, IAsyncDisposable
{

    public virtual ValueTask DisposeAsync()
    {
        DisposedCount += 1;
        return default(ValueTask);
    }

    public int DisposedCount
    {
        get;
        private set;
    }

    public virtual Task InitializeAsync()
    {
        InitializedCount += 1;
        return Task.CompletedTask;
    }

    public int InitializedCount
    {
        get;
        private set;
    }

    public bool IsInitialized => InitializedCount > 0;
    public bool IsDisposed => DisposedCount > 0;
}
