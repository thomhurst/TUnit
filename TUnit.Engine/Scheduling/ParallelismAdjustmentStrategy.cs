using System.Collections.Concurrent;
using TUnit.Engine.Services;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Strategy for adjusting parallelism based on system metrics
/// </summary>
internal sealed class ParallelismAdjustmentStrategy
{
    private readonly int _minParallelism;
    private readonly int _maxParallelism;
    private readonly ConcurrentQueue<TestCompletionInfo> _completedTests = new();
    private DateTime _lastMeasurementTime = DateTime.UtcNow;
    private int _lastCompletedCount;
    private const double CpuLowThreshold = 70.0;
    private const double CpuHighThreshold = 90.0;
    private const double MinIncreaseFactor = 0.25; // 25% increase (more aggressive)
    private const double MinDecreaseFactor = 0.15; // 15% decrease (more conservative)

    public ParallelismAdjustmentStrategy(int minParallelism, int maxParallelism)
    {
        _minParallelism = Math.Max(1, minParallelism);
        _maxParallelism = Math.Max(_minParallelism, maxParallelism);
    }

    /// <summary>
    /// Records a test completion for rate calculation
    /// </summary>
    public void RecordTestCompletion(TimeSpan executionTime)
    {
        _completedTests.Enqueue(new TestCompletionInfo
        {
            CompletionTime = DateTime.UtcNow,
            ExecutionTime = executionTime
        });

        // Clean up old entries (older than 10 seconds)
        var cutoff = DateTime.UtcNow.AddSeconds(-10);
        while (_completedTests.TryPeek(out var oldest) && oldest.CompletionTime < cutoff)
        {
            _completedTests.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Calculates the recommended parallelism adjustment
    /// </summary>
    public AdjustmentRecommendation CalculateAdjustment(SystemMetrics metrics, int currentParallelism)
    {
        var decision = MakeDecision(metrics, currentParallelism);
        
        // No sliding window - make immediate adjustments
        if (decision.Direction == AdjustmentDirection.None)
        {
            return new AdjustmentRecommendation
            {
                NewParallelism = currentParallelism,
                Direction = AdjustmentDirection.None,
                Reason = decision.Reason
            };
        }

        // Calculate new parallelism based on direction and current metrics
        int newParallelism;
        if (decision.Direction == AdjustmentDirection.Increase)
        {
            // Calculate optimal increase based on current CPU usage
            // If we're at 2% CPU with 176 tests, and we want to reach ~70% CPU,
            // we can estimate: newParallelism = currentParallelism * (targetCPU / currentCPU)
            var currentCpu = metrics.SystemCpuUsagePercent;
            if (currentCpu > 0 && currentCpu < 10.0) // Very low CPU usage
            {
                // Aggressive scaling: try to reach 60% CPU utilization
                var scaleFactor = Math.Min(60.0 / currentCpu, 3.0); // Cap at 3x to avoid overshooting
                var targetParallelism = (int)(currentParallelism * scaleFactor);
                newParallelism = Math.Min(_maxParallelism, targetParallelism);
            }
            else
            {
                // Normal increase by 25%
                var adjustmentSize = Math.Max(1, (int)(currentParallelism * MinIncreaseFactor));
                newParallelism = Math.Min(_maxParallelism, currentParallelism + adjustmentSize);
            }
        }
        else
        {
            // Decrease more conservatively
            var adjustmentSize = Math.Max(1, (int)(currentParallelism * MinDecreaseFactor));
            newParallelism = Math.Max(_minParallelism, currentParallelism - adjustmentSize);
        }

        return new AdjustmentRecommendation
        {
            NewParallelism = newParallelism,
            Direction = decision.Direction,
            Reason = decision.Reason
        };
    }

    private AdjustmentDecision MakeDecision(SystemMetrics metrics, int currentParallelism)
    {
        // Check for thread pool starvation
        var threadUtilization = CalculateThreadUtilization(metrics);
        if (threadUtilization > 0.9 || metrics.PendingWorkItems > 100)
        {
            return new AdjustmentDecision
            {
                Direction = AdjustmentDirection.Decrease,
                Reason = $"Thread pool starvation detected (utilization: {threadUtilization:P1}, pending: {metrics.PendingWorkItems})"
            };
        }

        // Check CPU usage
        if (metrics.SystemCpuUsagePercent > CpuHighThreshold)
        {
            return new AdjustmentDecision
            {
                Direction = AdjustmentDirection.Decrease,
                Reason = $"High CPU usage ({metrics.SystemCpuUsagePercent:F1}%)"
            };
        }

        // Check memory pressure
        if (metrics.TotalMemoryBytes > 1_000_000_000) // Over 1GB
        {
            return new AdjustmentDecision
            {
                Direction = AdjustmentDirection.Decrease,
                Reason = $"High memory usage ({metrics.TotalMemoryBytes / 1_000_000}MB)"
            };
        }

        // Calculate test completion rate
        var completionRate = CalculateCompletionRate();
        
        // Check if we can increase parallelism
        if (metrics.SystemCpuUsagePercent < CpuLowThreshold && 
            threadUtilization < 0.7 &&
            currentParallelism < _maxParallelism)
        {
            // Check if completion rate is stable or improving
            if (completionRate >= 0) // Not declining
            {
                return new AdjustmentDecision
                {
                    Direction = AdjustmentDirection.Increase,
                    Reason = $"Resources available (CPU: {metrics.SystemCpuUsagePercent:F1}%, threads: {threadUtilization:P1})"
                };
            }
        }

        // If completion rate is declining significantly, decrease
        if (completionRate < -0.2) // More than 20% decline
        {
            return new AdjustmentDecision
            {
                Direction = AdjustmentDirection.Decrease,
                Reason = $"Test completion rate declining ({completionRate:P1})"
            };
        }

        return new AdjustmentDecision
        {
            Direction = AdjustmentDirection.None,
            Reason = "System metrics stable"
        };
    }

    private double CalculateThreadUtilization(SystemMetrics metrics)
    {
        if (metrics.MaxWorkerThreads == 0) return 0;
        return 1.0 - (double)metrics.AvailableWorkerThreads / metrics.MaxWorkerThreads;
    }

    private double CalculateCompletionRate()
    {
        var now = DateTime.UtcNow;
        var timeDelta = (now - _lastMeasurementTime).TotalSeconds;
        if (timeDelta < 1) return 0; // Not enough time passed

        var currentCount = _completedTests.Count;
        var completedInPeriod = currentCount - _lastCompletedCount;
        
        var currentRate = completedInPeriod / timeDelta;
        var previousRate = _lastCompletedCount / 10.0; // Over 10 second window
        
        _lastCompletedCount = currentCount;
        _lastMeasurementTime = now;

        if (previousRate == 0) return 0;
        return (currentRate - previousRate) / previousRate; // Percentage change
    }

    private sealed class AdjustmentDecision
    {
        public AdjustmentDirection Direction { get; init; }
        public string Reason { get; init; } = "";
    }

    private sealed class TestCompletionInfo
    {
        public DateTime CompletionTime { get; init; }
        public TimeSpan ExecutionTime { get; init; }
    }
}

/// <summary>
/// Adjustment direction
/// </summary>
internal enum AdjustmentDirection
{
    None,
    Increase,
    Decrease
}

/// <summary>
/// Adjustment recommendation
/// </summary>
internal sealed class AdjustmentRecommendation
{
    public int NewParallelism { get; init; }
    public AdjustmentDirection Direction { get; init; }
    public string Reason { get; init; } = "";
}