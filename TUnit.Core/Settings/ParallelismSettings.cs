namespace TUnit.Core.Settings;

/// <summary>
/// Controls concurrent test execution.
/// </summary>
public sealed class ParallelismSettings
{
    /// <summary>
    /// Maximum number of tests to run in parallel. Default: <c>null</c> (= 4× CPU cores).
    /// Precedence: <c>--maximum-parallel-tests</c> → <c>TUNIT_MAX_PARALLEL_TESTS</c> → TUnitSettings → built-in default.
    /// <para>
    /// <b>Note:</b> This value is read during scheduler initialization, which occurs before
    /// <c>[Before(HookType.TestDiscovery)]</c> hooks run. Set this value in a module initializer
    /// or static constructor, or use the <c>--maximum-parallel-tests</c> CLI flag /
    /// <c>TUNIT_MAX_PARALLEL_TESTS</c> environment variable instead.
    /// </para>
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
