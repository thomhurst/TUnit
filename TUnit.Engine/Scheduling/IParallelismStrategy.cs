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
