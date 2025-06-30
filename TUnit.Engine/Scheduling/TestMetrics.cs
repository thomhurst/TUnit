namespace TUnit.Engine.Scheduling;

/// <summary>
/// Metrics for individual test execution
/// </summary>
public sealed class TestMetrics
{
    public string TestId { get; init; } = string.Empty;
    public TimeSpan WaitTime { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public int RetryCount { get; set; }
    public bool Success { get; set; }
}
