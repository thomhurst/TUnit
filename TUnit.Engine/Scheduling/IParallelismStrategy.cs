namespace TUnit.Engine.Scheduling;

/// <summary>
/// Defines the contract for parallelism adaptation strategies
/// </summary>
public interface IParallelismStrategy
{
    /// <summary>
    /// Gets the current parallelism level
    /// </summary>
    int CurrentParallelism { get; }
    
    /// <summary>
    /// Adapts parallelism based on system metrics
    /// </summary>
    void AdaptParallelism(ParallelismMetrics metrics);
}

/// <summary>
/// Metrics for parallelism adaptation
/// </summary>
public sealed class ParallelismMetrics
{
    public double CpuUsage { get; init; }
    public int QueueDepth { get; init; }
    public int ActiveThreads { get; init; }
    public double AverageTestDuration { get; init; }
}