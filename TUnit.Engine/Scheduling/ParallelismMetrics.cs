namespace TUnit.Engine.Scheduling;

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