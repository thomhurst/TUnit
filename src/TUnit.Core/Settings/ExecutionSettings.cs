namespace TUnit.Core.Settings;

/// <summary>
/// Controls test run behavior.
/// </summary>
public sealed class ExecutionSettings
{
    internal ExecutionSettings() { }

    /// <summary>
    /// Whether to cancel the test run after the first test failure. Default: <c>false</c>.
    /// Precedence: <c>--fail-fast</c> → TUnitSettings → built-in default.
    /// </summary>
    public bool FailFast { get; set; }

    /// <summary>
    /// Whether TUnit raises the thread pool's minimum worker thread count when blocking tests
    /// starve the pool. Starvation delays timers and <c>[Timeout]</c> handling in unrelated tests.
    /// The minimum is raised only after starvation is detected, so suites that never block are
    /// unaffected. Default: <c>true</c>.
    /// Precedence: <c>TUNIT_DISABLE_THREADPOOL_TUNING</c> → TUnitSettings → built-in default.
    /// </summary>
    public bool ThreadPoolTuningEnabled { get; set; } = true;
}
