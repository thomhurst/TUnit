namespace TUnit.TestProject.Bugs._1410;

public class SharedFixture : IDisposable
{
    public bool IsDisposed { get; private set; } = false;

    public void Dispose()
    {
        IsDisposed = true;
    }
}