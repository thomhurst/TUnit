namespace TUnit.Core.Settings;

/// <summary>
/// Controls concurrent test execution.
/// </summary>
public sealed class ParallelismSettings
{
    internal ParallelismSettings() { }

    /// <summary>
    /// Maximum number of tests to run in parallel. Default: <c>null</c> (= 2× CPU cores).
    /// Precedence: <c>--maximum-parallel-tests</c> → <c>TUNIT_MAX_PARALLEL_TESTS</c> → TUnitSettings → built-in default.
    /// </summary>
    public int? MaximumParallelTests
    {
        get => _maximumParallelTests;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "MaximumParallelTests must be null, 0 (unlimited), or a positive number.");
            }

            _maximumParallelTests = value;
        }
    }

    private int? _maximumParallelTests;

    /// <summary>
    /// Whether TUnit changes the number of tests that run in parallel while the run is in progress.
    /// It raises the limit when the machine has CPU and memory to spare, lowers it when the CPUs
    /// are saturated, and keeps either change only if it completes more tests per second (or, for
    /// a lowering, no fewer). Memory pressure lowers the limit at once.
    /// <see cref="MaximumParallelTests"/>, when set, is where the limit starts and the highest it
    /// goes; otherwise it starts at 2× CPU cores and may reach 8×. Default: <c>false</c>.
    /// Enabled by any of <c>--adaptive-parallelism</c>, <c>TUNIT_ADAPTIVE_PARALLELISM</c> or TUnitSettings.
    /// </summary>
    public bool Adaptive { get; set; }
}
