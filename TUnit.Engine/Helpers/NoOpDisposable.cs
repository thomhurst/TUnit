namespace TUnit.Engine.Helpers;

internal class NoOpDisposable : IDisposable
{
    public static readonly NoOpDisposable Instance = new();

    private NoOpDisposable()
    {
    }

    public void Dispose()
    {
    }
}
