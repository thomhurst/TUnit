namespace TUnit.TestProject.Bugs._1410;

public class SharedFixture : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}