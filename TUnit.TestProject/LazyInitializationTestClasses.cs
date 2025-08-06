using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

/// <summary>
/// Test implementation that tracks initialization for testing lazy loading behavior.
/// </summary>
public class LazyTestServer : IAsyncInitializer, IRequiresLazyInitialization, IAsyncDisposable
{
    private static int _initializationCount = 0;
    private static readonly List<string> _initializationLog = new();
    
    public bool IsInitialized { get; private set; }
    public string InstanceId { get; } = Guid.NewGuid().ToString("N")[..8];
    
    public static int InitializationCount => _initializationCount;
    public static IReadOnlyList<string> InitializationLog => _initializationLog.AsReadOnly();
    
    public static void ResetCounters()
    {
        _initializationCount = 0;
        _initializationLog.Clear();
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        Interlocked.Increment(ref _initializationCount);
        
        lock (_initializationLog)
        {
            _initializationLog.Add($"Initialized LazyTestServer {InstanceId} at {DateTime.Now:HH:mm:ss.fff}");
        }
        
        // Simulate expensive initialization
        await Task.Delay(50);
        
        IsInitialized = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (IsInitialized)
        {
            lock (_initializationLog)
            {
                _initializationLog.Add($"Disposed LazyTestServer {InstanceId} at {DateTime.Now:HH:mm:ss.fff}");
            }
        }
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// Test implementation that does NOT implement IRequiresLazyInitialization for comparison.
/// </summary>
public class EagerTestServer : IAsyncInitializer, IAsyncDisposable
{
    private static int _initializationCount = 0;
    private static readonly List<string> _initializationLog = new();
    
    public bool IsInitialized { get; private set; }
    public string InstanceId { get; } = Guid.NewGuid().ToString("N")[..8];
    
    public static int InitializationCount => _initializationCount;
    public static IReadOnlyList<string> InitializationLog => _initializationLog.AsReadOnly();
    
    public static void ResetCounters()
    {
        _initializationCount = 0;
        _initializationLog.Clear();
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        Interlocked.Increment(ref _initializationCount);
        
        lock (_initializationLog)
        {
            _initializationLog.Add($"Initialized EagerTestServer {InstanceId} at {DateTime.Now:HH:mm:ss.fff}");
        }
        
        // Simulate expensive initialization  
        await Task.Delay(50);
        
        IsInitialized = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (IsInitialized)
        {
            lock (_initializationLog)
            {
                _initializationLog.Add($"Disposed EagerTestServer {InstanceId} at {DateTime.Now:HH:mm:ss.fff}");
            }
        }
        
        await Task.CompletedTask;
    }
}