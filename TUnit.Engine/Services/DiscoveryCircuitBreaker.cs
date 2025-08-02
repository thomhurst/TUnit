using System.Diagnostics;

namespace TUnit.Engine.Services;

/// <summary>
/// Intelligent circuit breaker for test discovery that uses memory and time-based safeguards
/// instead of arbitrary count limits
/// </summary>
public sealed class DiscoveryCircuitBreaker
{
    private readonly long _maxMemoryBytes;
    private readonly TimeSpan _maxGenerationTime;
    private readonly Stopwatch _stopwatch;
    private readonly long _initialMemoryUsage;

    /// <summary>
    /// Creates a new discovery circuit breaker with intelligent limits
    /// </summary>
    public DiscoveryCircuitBreaker(
        double maxMemoryPercentage = 0.7, // Use up to 70% of available memory
        TimeSpan? maxGenerationTime = null) // Default 2 minutes
    {
        _maxMemoryBytes = (long)(GetAvailableMemoryBytes() * maxMemoryPercentage);
        _maxGenerationTime = maxGenerationTime ?? TimeSpan.FromMinutes(2);
        _stopwatch = Stopwatch.StartNew();
        
        // Track initial memory to calculate growth
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        _initialMemoryUsage = GC.GetTotalMemory(false);
    }

    /// <summary>
    /// Checks if the circuit breaker should trip based on current resource usage
    /// </summary>
    /// <param name="currentTestCount">Current number of generated tests (for logging only)</param>
    /// <returns>True if generation should continue, false if circuit breaker trips</returns>
    public bool ShouldContinue(int currentTestCount = 0)
    {
        if (_stopwatch.Elapsed > _maxGenerationTime)
        {
            return false;
        }

        var currentMemoryUsage = GC.GetTotalMemory(false);
        var memoryGrowth = currentMemoryUsage - _initialMemoryUsage;
        
        if (memoryGrowth > _maxMemoryBytes)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the current resource usage statistics
    /// </summary>
    public DiscoveryResourceUsage GetResourceUsage()
    {
        var currentMemoryUsage = GC.GetTotalMemory(false);
        var memoryGrowth = currentMemoryUsage - _initialMemoryUsage;
        
        return new DiscoveryResourceUsage
        {
            ElapsedTime = _stopwatch.Elapsed,
            MaxTime = _maxGenerationTime,
            MemoryGrowthBytes = memoryGrowth,
            MaxMemoryBytes = _maxMemoryBytes,
            TimeUsagePercentage = _stopwatch.Elapsed.TotalMilliseconds / _maxGenerationTime.TotalMilliseconds,
            MemoryUsagePercentage = (double)memoryGrowth / _maxMemoryBytes
        };
    }

    /// <summary>
    /// Creates a resource usage report for logging
    /// </summary>
    public string CreateResourceReport(int testCount)
    {
        var usage = GetResourceUsage();
        return $"Generated {testCount:N0} tests in {usage.ElapsedTime.TotalSeconds:F1}s " +
               $"({usage.TimeUsagePercentage:P1} of time limit), " +
               $"using {usage.MemoryGrowthBytes / (1024.0 * 1024.0):F1}MB " +
               $"({usage.MemoryUsagePercentage:P1} of memory limit)";
    }

    private static long GetAvailableMemoryBytes()
    {
        try
        {
#if NET6_0_OR_GREATER
            // Try to get actual available memory on newer .NET versions
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            if (gcMemoryInfo.TotalAvailableMemoryBytes > 0)
            {
                return gcMemoryInfo.TotalAvailableMemoryBytes;
            }
#endif
        }
        catch
        {
            // Fall back to conservative estimate
        }

        // Conservative fallback: assume 1GB available
        return 1024L * 1024L * 1024L;
    }

    public void Dispose()
    {
        _stopwatch?.Stop();
    }
}

/// <summary>
/// Resource usage statistics for discovery operations
/// </summary>
public record DiscoveryResourceUsage
{
    public TimeSpan ElapsedTime { get; init; }
    public TimeSpan MaxTime { get; init; }
    public long MemoryGrowthBytes { get; init; }
    public long MaxMemoryBytes { get; init; }
    public double TimeUsagePercentage { get; init; }
    public double MemoryUsagePercentage { get; init; }
}