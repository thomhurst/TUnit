namespace TUnit.TestProject.ObjectTracking;

public class Disposable : IDisposable
{
    public void Dispose()
    {
        IsDisposed = true;
    }

    public bool IsDisposed
    {
        get;
        private set;
    }
}
