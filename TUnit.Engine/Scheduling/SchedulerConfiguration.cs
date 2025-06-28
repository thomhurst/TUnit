using System;

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
    /// </summary>
    public int MaxParallelism { get; set; } = Environment.ProcessorCount * 2;
    
    /// <summary>
    /// Timeout for individual tests
    /// </summary>
    public TimeSpan TestTimeout { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Timeout for detecting execution stalls
    /// </summary>
    public TimeSpan StallTimeout { get; set; } = TimeSpan.FromMinutes(10);
    
    /// <summary>
    /// Whether to enable work stealing between threads
    /// </summary>
    public bool EnableWorkStealing { get; set; } = true;
    
    /// <summary>
    /// Whether to enable adaptive parallelism
    /// </summary>
    public bool EnableAdaptiveParallelism { get; set; } = true;
    
    /// <summary>
    /// Parallelism strategy to use
    /// </summary>
    public ParallelismStrategy Strategy { get; set; } = ParallelismStrategy.Adaptive;
    
    /// <summary>
    /// Creates default configuration
    /// </summary>
    public static SchedulerConfiguration Default => new();
}

/// <summary>
/// Available parallelism strategies
/// </summary>
public enum ParallelismStrategy
{
    /// <summary>
    /// Fixed parallelism based on processor count
    /// </summary>
    Fixed,
    
    /// <summary>
    /// Adaptive parallelism based on system metrics
    /// </summary>
    Adaptive
}