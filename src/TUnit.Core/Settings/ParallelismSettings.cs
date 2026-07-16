namespace TUnit.Core.Settings;

/// <summary>
/// Controls concurrent test execution.
/// </summary>
public sealed class ParallelismSettings
{
    internal ParallelismSettings() { }

    /// <summary>
    /// Maximum number of tests to run in parallel. Default: <c>null</c> (= 4× CPU cores).
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
}
