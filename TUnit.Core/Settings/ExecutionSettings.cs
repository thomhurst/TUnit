namespace TUnit.Core.Settings;

/// <summary>
/// Controls test run behavior.
/// </summary>
public sealed class ExecutionSettings
{
    /// <summary>
    /// Whether to cancel the test run after the first test failure. Default: <c>false</c>.
    /// Precedence: <c>--fail-fast</c> → TUnitSettings → built-in default.
    /// </summary>
    public bool FailFast { get; set; }
}
