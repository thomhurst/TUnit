namespace TUnit.Engine.Scheduling;

/// <summary>
/// Configuration options for test scheduler
/// </summary>
public sealed class SchedulerConfiguration
{
    /// <summary>
    /// Minimum number of parallel threads
    /// </summary>
    public int MinParallelism { get; set; } = 1;

    /// <summary>
    /// Maximum number of parallel threads
    /// Default: 4x processor count for optimal test throughput (most tests are I/O bound)
    /// </summary>
    public int MaxParallelism { get; set; } = Environment.ProcessorCount * 4;

    /// <summary>
    /// Timeout for individual tests
    /// </summary>
    public TimeSpan TestTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Timeout for detecting execution stalls
    /// </summary>
    public TimeSpan StallTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Parallelism strategy to use
    /// </summary>
    public ParallelismStrategy Strategy { get; set; } = ParallelismStrategy.Adaptive;

    /// <summary>
    /// Minimum parallelism for adaptive strategy
    /// </summary>
    public int AdaptiveMinParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Maximum parallelism for adaptive strategy
    /// Default: 64x processor count for I/O-bound tests (can be overridden via command-line or environment variable)
    /// </summary>
    public int AdaptiveMaxParallelism { get; set; } = Environment.ProcessorCount * 64;

    /// <summary>
    /// Enable detailed metrics logging for adaptive strategy
    /// </summary>
    public bool EnableAdaptiveMetrics { get; set; } = false;

    /// <summary>
    /// Creates default configuration
    /// </summary>
    public static SchedulerConfiguration Default => new();
}
