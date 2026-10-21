namespace TUnit.Engine.Scheduling;

/// <summary>
/// The session-wide --maximum-parallel-tests budget. The unconstrained phase, the keyed
/// NotInParallel phase and dynamic test batches can all run at the same time, so they
/// must draw from one budget for the configured cap to hold across all of them (#7074).
/// </summary>
/// <remarks>
/// Take a slot only immediately before entering <see cref="TestRunner"/>: never while
/// waiting on constraint keys. A dependency that a slot holder reaches first runs inline
/// within that holder's slot (via the <see cref="TestRunner"/> dedup ledger), so a slot
/// holder never waits on a test that still needs a slot of its own.
/// </remarks>
internal sealed class MaxParallelismLimiter
{
    private readonly Lazy<int> _maxParallelism;
    private readonly Lazy<SemaphoreSlim> _semaphore;

    /// <summary>
    /// The cap used when nothing configures one, and for "unlimited" (0). ProcessorCount * 4
    /// is empirically sized for async/IO-bound workloads.
    /// </summary>
    public static int DefaultMaxParallelism => Environment.ProcessorCount * 4;

    public MaxParallelismLimiter(Func<int> resolveMaxParallelism)
    {
        _maxParallelism = new Lazy<int>(resolveMaxParallelism);
        _semaphore = new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(_maxParallelism.Value, _maxParallelism.Value));
    }

    public int MaxParallelism => _maxParallelism.Value;

    public SemaphoreSlim Semaphore => _semaphore.Value;
}
