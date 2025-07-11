namespace TUnit.Engine.Scheduling;

/// <summary>
/// Configuration options for worker thread behavior
/// </summary>
public class WorkerThreadOptions
{
    /// <summary>
    /// Maximum number of spurious wakeups before increasing timeout
    /// </summary>
    public int MaxSpuriousWakeups { get; set; } = 10;
    
    /// <summary>
    /// Window for batching notifications in microseconds
    /// </summary>
    public int NotificationBatchWindowMicroseconds { get; set; } = 100;
    
    /// <summary>
    /// Whether to enable work stealing between workers
    /// </summary>
    public bool EnableWorkStealing { get; set; } = true;
    
    /// <summary>
    /// Number of items in local queue before stealing is attempted
    /// </summary>
    public int WorkStealingThreshold { get; set; } = 2;
}