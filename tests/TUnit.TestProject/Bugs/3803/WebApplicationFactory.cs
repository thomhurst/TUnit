using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._3803;

/// <summary>
/// Simulates a WebApplicationFactory that depends on test containers.
/// The containers should be shared (SharedType.PerTestSession), meaning:
/// - Each container should be instantiated only ONCE per test session
/// - All instances of WebApplicationFactory should receive the SAME container instances
/// </summary>
public class WebApplicationFactory : IAsyncInitializer, IAsyncDisposable
{
    private static int _instanceCount = 0;
    private static int _initializeCount = 0;
    private static int _disposeCount = 0;

    public static int InstanceCount => _instanceCount;
    public static int InitializeCount => _initializeCount;
    public static int DisposeCount => _disposeCount;

    public int InstanceId { get; }
    public bool IsInitialized { get; private set; }
    public bool IsDisposed { get; private set; }

    [ClassDataSource<TestRabbitContainer>(Shared = SharedType.PerTestSession)]
    public required TestRabbitContainer RabbitContainer { get; init; }

    [ClassDataSource<TestSqlContainer>(Shared = SharedType.PerTestSession)]
    public required TestSqlContainer SqlContainer { get; init; }

    public WebApplicationFactory()
    {
        InstanceId = Interlocked.Increment(ref _instanceCount);
        Console.WriteLine($"[WebApplicationFactory] Constructor called - Instance #{InstanceId}");
    }

    public Task InitializeAsync()
    {
        Interlocked.Increment(ref _initializeCount);
        IsInitialized = true;
        Console.WriteLine($"[WebApplicationFactory] InitializeAsync called - Instance #{InstanceId}");
        Console.WriteLine($"  -> RabbitContainer Instance: #{RabbitContainer.InstanceId}");
        Console.WriteLine($"  -> SqlContainer Instance: #{SqlContainer.InstanceId}");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _disposeCount);
        IsDisposed = true;
        Console.WriteLine($"[WebApplicationFactory] DisposeAsync called - Instance #{InstanceId}");
        return default;
    }

    public static void ResetCounters()
    {
        _instanceCount = 0;
        _initializeCount = 0;
        _disposeCount = 0;
    }
}
