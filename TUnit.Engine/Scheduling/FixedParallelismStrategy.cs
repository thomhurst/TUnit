namespace TUnit.Engine.Scheduling;

/// <summary>
/// Fixed parallelism strategy for simpler scenarios
/// </summary>
public sealed class FixedParallelismStrategy : IParallelismStrategy
{
    private readonly int _parallelism;

    public FixedParallelismStrategy(int? parallelism = null)
    {
        _parallelism = parallelism ?? Environment.ProcessorCount;
    }

    public int CurrentParallelism => _parallelism;

    public void AdaptParallelism(ParallelismMetrics metrics)
    {
        // No adaptation for fixed strategy
    }
}
