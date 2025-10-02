namespace TUnit.Engine.Scheduling;

/// <summary>
/// Available parallelism strategies
/// </summary>
internal enum ParallelismStrategy
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
