using System.Diagnostics;

namespace TUnit.Engine.Services;

/// <summary>
/// Collects system metrics for adaptive parallelism
/// </summary>
internal sealed class SystemMetricsCollector : IDisposable
{
    private readonly Process _currentProcess;
    private readonly Timer _gcMemoryTimer;
    private long _lastGcMemory;
    private DateTime _lastCpuTime;
    private TimeSpan _lastTotalProcessorTime;
    private double _lastCpuUsage;

    public SystemMetricsCollector()
    {
        _currentProcess = Process.GetCurrentProcess();
        _lastCpuTime = DateTime.UtcNow;
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;

        // Update GC memory periodically to avoid blocking
        _gcMemoryTimer = new Timer(_ => _lastGcMemory = GC.GetTotalMemory(false), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Gets current system metrics snapshot
    /// </summary>
    public SystemMetrics GetMetrics()
    {
        var now = DateTime.UtcNow;
        var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;
        var timeDelta = (now - _lastCpuTime).TotalMilliseconds;
        
        double processCpuUsage = 0;
        if (timeDelta > 0)
        {
            var cpuTimeDelta = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
            processCpuUsage = (cpuTimeDelta / timeDelta) / Environment.ProcessorCount * 100;
            _lastCpuUsage = processCpuUsage;
        }
        else
        {
            processCpuUsage = _lastCpuUsage;
        }

        _lastCpuTime = now;
        _lastTotalProcessorTime = currentTotalProcessorTime;

        // Thread pool statistics
        ThreadPool.GetAvailableThreads(out var workerThreads, out var ioThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxIoThreads);
        long pendingWorkItems = 0;
#if NET5_0_OR_GREATER
        pendingWorkItems = ThreadPool.PendingWorkItemCount;
#endif

        // Memory metrics
        var totalMemory = GC.GetTotalMemory(false);
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);

        return new SystemMetrics
        {
            ProcessCpuUsagePercent = processCpuUsage,
            SystemCpuUsagePercent = processCpuUsage, // Use process CPU as approximation
            AvailableWorkerThreads = workerThreads,
            AvailableIoThreads = ioThreads,
            MaxWorkerThreads = maxWorkerThreads,
            MaxIoThreads = maxIoThreads,
            PendingWorkItems = pendingWorkItems,
            TotalMemoryBytes = totalMemory,
            Gen0Collections = gen0Collections,
            Gen1Collections = gen1Collections,
            Gen2Collections = gen2Collections,
            Timestamp = now
        };
    }

    /// <summary>
    /// Detects if the thread pool is experiencing starvation
    /// </summary>
    public bool IsThreadPoolStarved()
    {
        ThreadPool.GetAvailableThreads(out var workerThreads, out var ioThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxIoThreads);
        
        // Consider starved if less than 10% of threads available
        var workerUtilization = 1.0 - (double)workerThreads / maxWorkerThreads;
        var ioUtilization = 1.0 - (double)ioThreads / maxIoThreads;
        
        bool hasPendingWork = false;
#if NET5_0_OR_GREATER
        hasPendingWork = ThreadPool.PendingWorkItemCount > 100;
#endif
        return workerUtilization > 0.9 || ioUtilization > 0.9 || hasPendingWork;
    }

    /// <summary>
    /// Detects memory pressure
    /// </summary>
    public bool IsMemoryPressureHigh()
    {
        // Simple heuristic: check if we're using more than 90% of max generation size
        // or if Gen2 collections are happening frequently
        var totalMemory = GC.GetTotalMemory(false);
        var gen2Size = GC.GetGeneration(new object()) == 2 ? totalMemory : 0;
        
        // Check if memory is growing rapidly
        var memoryGrowth = totalMemory - _lastGcMemory;
        _lastGcMemory = totalMemory;
        
        return totalMemory > 1_000_000_000 || // Over 1GB
               memoryGrowth > 100_000_000; // Growing by more than 100MB/sec
    }

    public void Dispose()
    {
        _gcMemoryTimer?.Dispose();
        _currentProcess?.Dispose();
    }
}

/// <summary>
/// System metrics snapshot
/// </summary>
internal sealed class SystemMetrics
{
    public double ProcessCpuUsagePercent { get; init; }
    public double SystemCpuUsagePercent { get; init; }
    public int AvailableWorkerThreads { get; init; }
    public int AvailableIoThreads { get; init; }
    public int MaxWorkerThreads { get; init; }
    public int MaxIoThreads { get; init; }
    public long PendingWorkItems { get; init; }
    public long TotalMemoryBytes { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public DateTime Timestamp { get; init; }
}