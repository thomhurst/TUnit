namespace TUnit.Engine.Scheduling;

/// <summary>
/// Tracks execution metrics for monitoring and optimization
/// </summary>
public sealed class ExecutionMetrics
{
    public int TestsCompleted { get; set; }
    public int TestsInProgress { get; set; }
    public int TestsFailed { get; set; }
    public int TestsSkipped { get; set; }
    public double AverageParallelism { get; set; }
    public TimeSpan AverageWaitTime { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public Dictionary<string, TestMetrics> PerTestMetrics { get; } = new();
}
