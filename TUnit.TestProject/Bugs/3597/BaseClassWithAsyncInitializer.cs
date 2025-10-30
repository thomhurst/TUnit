using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._3597;

public abstract class BaseClassWithAsyncInitializer : IAsyncInitializer, IAsyncDisposable
{
    public abstract TestHost Host { get; set; }

    public bool WasInitialized { get; private set; }

    public Task InitializeAsync()
    {
        // This should not throw - Host should be set before InitializeAsync is called
        if (Host == null)
        {
            throw new InvalidOperationException("Host property is null in InitializeAsync - data source was not initialized before calling InitializeAsync");
        }

        WasInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return default;
    }
}
