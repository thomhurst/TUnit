using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._3803;

/// <summary>
/// Simulates a RabbitMQ test container.
/// This class should be instantiated only once per test session when marked as SharedType.PerTestSession.
/// </summary>
public class TestRabbitContainer : IAsyncInitializer, IAsyncDisposable
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

    public TestRabbitContainer()
    {
        InstanceId = Interlocked.Increment(ref _instanceCount);
        Console.WriteLine($"[TestRabbitContainer] Constructor called - Instance #{InstanceId}");
    }

    public Task InitializeAsync()
    {
        Interlocked.Increment(ref _initializeCount);
        IsInitialized = true;
        Console.WriteLine($"[TestRabbitContainer] InitializeAsync called - Instance #{InstanceId}");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _disposeCount);
        IsDisposed = true;
        Console.WriteLine($"[TestRabbitContainer] DisposeAsync called - Instance #{InstanceId}");
        return default;
    }

    public static void ResetCounters()
    {
        _instanceCount = 0;
        _initializeCount = 0;
        _disposeCount = 0;
    }
}
