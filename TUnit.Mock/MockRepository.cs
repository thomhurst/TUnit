namespace TUnit.Mock;

/// <summary>
/// A container that creates and tracks mocks, enabling batch operations
/// like <see cref="VerifyAll"/>, <see cref="VerifyNoOtherCalls"/>, and <see cref="Reset"/>.
/// </summary>
public class MockRepository
{
    private readonly MockBehavior _defaultBehavior;
    private readonly List<IMock> _mocks = new();
    private readonly object _lock = new();

    /// <summary>Creates a repository with <see cref="MockBehavior.Loose"/> as the default behavior.</summary>
    public MockRepository() : this(MockBehavior.Loose) { }

    /// <summary>Creates a repository with the specified default behavior for all mocks created through it.</summary>
    public MockRepository(MockBehavior defaultBehavior)
    {
        _defaultBehavior = defaultBehavior;
    }

    /// <summary>Creates and tracks a mock of T using the repository's default behavior.</summary>
    public Mock<T> Of<T>() where T : class => Of<T>(_defaultBehavior);

    /// <summary>Creates and tracks a mock of T with the specified behavior.</summary>
    public Mock<T> Of<T>(MockBehavior behavior) where T : class
    {
        var mock = Mock.Of<T>(behavior);
        Track(mock);
        return mock;
    }

    /// <summary>Creates and tracks a partial mock of T using the repository's default behavior.</summary>
    public Mock<T> OfPartial<T>(params object[] constructorArgs) where T : class
        => OfPartial<T>(_defaultBehavior, constructorArgs);

    /// <summary>Creates and tracks a partial mock of T with the specified behavior.</summary>
    public Mock<T> OfPartial<T>(MockBehavior behavior, params object[] constructorArgs) where T : class
    {
        var mock = Mock.OfPartial<T>(behavior, constructorArgs);
        Track(mock);
        return mock;
    }

    /// <summary>Adds an existing mock to this repository for batch operations.</summary>
    public void Track(IMock mock)
    {
        lock (_lock)
        {
            _mocks.Add(mock);
        }
    }

    /// <summary>All mocks tracked by this repository.</summary>
    public IReadOnlyList<IMock> Mocks
    {
        get
        {
            lock (_lock)
            {
                return _mocks.ToArray();
            }
        }
    }

    /// <summary>
    /// Calls <see cref="IMock.VerifyAll"/> on every tracked mock.
    /// Throws on the first mock that has uninvoked setups.
    /// </summary>
    public void VerifyAll()
    {
        foreach (var mock in GetSnapshot())
        {
            mock.VerifyAll();
        }
    }

    /// <summary>
    /// Calls <see cref="IMock.VerifyNoOtherCalls"/> on every tracked mock.
    /// Throws on the first mock that has unverified calls.
    /// </summary>
    public void VerifyNoOtherCalls()
    {
        foreach (var mock in GetSnapshot())
        {
            mock.VerifyNoOtherCalls();
        }
    }

    /// <summary>
    /// Calls <see cref="IMock.Reset"/> on every tracked mock, clearing all setups and call history.
    /// </summary>
    public void Reset()
    {
        foreach (var mock in GetSnapshot())
        {
            mock.Reset();
        }
    }

    private IMock[] GetSnapshot()
    {
        lock (_lock)
        {
            return _mocks.ToArray();
        }
    }
}
