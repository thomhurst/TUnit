namespace TUnit.TestProject.Library.Models;

public class SomeAsyncDisposableClass : IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public int Value => 1;

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default(ValueTask);
    }
}
