namespace TUnit.Engine.Scheduling;

/// <summary>
/// Monitors test execution progress and detects stalls
/// </summary>
public interface IProgressMonitor
{
    /// <summary>
    /// Monitors progress and throws if execution stalls
    /// </summary>
    Task MonitorProgressAsync(TestCompletionTracker tracker, CancellationToken cancellationToken);
}
