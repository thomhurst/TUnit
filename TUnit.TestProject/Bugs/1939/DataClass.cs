using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._1939;

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

    public ValueTask CalledOnTestClassDisposal()
    {
        if (Disposed)
        {
            throw new ObjectDisposedException(nameof(DataClass));
        }

        Console.WriteLine("This method is called when the test class is disposed");

        return default;
    }
}