namespace TUnit.Core.Settings;

/// <summary>
/// Controls concurrent test execution.
/// </summary>
public sealed class ParallelismSettings
{
    /// <summary>
    /// Maximum number of tests to run in parallel. Default: <c>null</c> (= 4× CPU cores).
    /// Precedence: <c>--maximum-parallel-tests</c> → <c>TUNIT_MAX_PARALLEL_TESTS</c> → TUnitSettings → built-in default.
    /// </summary>
    public int? MaximumParallelTests { get; set; }
}
