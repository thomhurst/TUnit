#pragma warning disable CS9113 // Parameter is unread.
namespace TUnit.TestProject;

public abstract class DisposedReproTestBase : IDisposable
{
    private bool _disposed;

    public void CheckDisposed()
    {
        if (_disposed)
        {
            throw new InvalidOperationException("Already disposed");
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}

[ClassDataSource<Dummy2>]
[NotInParallel]
public sealed class DisposedRepro(Dummy2 dummy) : DisposedReproTestBase
{
    [Test]
    [MethodDataSource(nameof(GetValues))]
    public void DoTest(int value)
    {
        CheckDisposed();
    }

    public static IEnumerable<int> GetValues() => [1, 2, 3];
}

public sealed record Dummy2;