namespace TUnit.Engine.Scheduling;

/// <summary>
/// Adapts parallelism based on system metrics using hill-climbing algorithm
/// </summary>
public sealed class AdaptiveParallelismStrategy : IParallelismStrategy
{
    private readonly int _minParallelism;
    private readonly int _maxParallelism;
    private readonly object _lock = new();
    private int _currentParallelism;
    private double _lastThroughput;
    private DateTime _lastAdjustment;
    private readonly TimeSpan _adjustmentInterval;

    public AdaptiveParallelismStrategy(
        int? minParallelism = null,
        int? maxParallelism = null,
        TimeSpan? adjustmentInterval = null)
    {
        _minParallelism = minParallelism ?? 1;
        _maxParallelism = maxParallelism ?? Environment.ProcessorCount * 2;
        _currentParallelism = Environment.ProcessorCount;
        _adjustmentInterval = adjustmentInterval ?? TimeSpan.FromSeconds(5);
        _lastAdjustment = DateTime.UtcNow;
    }

    public int CurrentParallelism
    {
        get
        {
            lock (_lock)
            {
                return _currentParallelism;
            }
        }
    }

    public void AdaptParallelism(ParallelismMetrics metrics)
    {
        lock (_lock)
        {
            if (DateTime.UtcNow - _lastAdjustment < _adjustmentInterval)
            {
                return;
            }

            var currentThroughput = CalculateThroughput(metrics);

            // Hill-climbing algorithm
            if (ShouldIncreaseParallelism(metrics))
            {
                _currentParallelism = Math.Min(_currentParallelism + 1, _maxParallelism);
            }
            else if (ShouldDecreaseParallelism(metrics))
            {
                _currentParallelism = Math.Max(_currentParallelism - 1, _minParallelism);
            }

            _lastThroughput = currentThroughput;
            _lastAdjustment = DateTime.UtcNow;
        }
    }

    private double CalculateThroughput(ParallelismMetrics metrics)
    {
        // Simple throughput calculation
        return metrics.ActiveThreads / Math.Max(metrics.AverageTestDuration, 0.001);
    }

    private bool ShouldIncreaseParallelism(ParallelismMetrics metrics)
    {
        // Increase if CPU usage is low and queue is deep
        return metrics.CpuUsage < 50 &&
               metrics.QueueDepth > metrics.ActiveThreads * 2 &&
               _currentParallelism < _maxParallelism;
    }

    private bool ShouldDecreaseParallelism(ParallelismMetrics metrics)
    {
        // Decrease if CPU usage is high and we have more threads than cores
        return metrics.CpuUsage > 90 &&
               metrics.ActiveThreads > Environment.ProcessorCount &&
               _currentParallelism > _minParallelism;
    }
}
