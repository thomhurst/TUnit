namespace TUnit.Core.Settings;

/// <summary>
/// Programmatic configuration for TUnit. Set these in a
/// <c>[Before(HookType.TestDiscovery)]</c> hook to establish project-level defaults.
/// <para>
/// Precedence: CLI flag → environment variable → <see cref="TUnitSettings"/> → built-in default.
/// </para>
/// <para>
/// <b>Threading:</b> All settings should be configured before test execution begins
/// (typically in a <c>[Before(HookType.TestDiscovery)]</c> hook). The framework ensures
/// hook completion happens-before test threads start, so no additional synchronization
/// is required. Modifying settings during parallel test execution is not supported.
/// </para>
/// </summary>
public static class TUnitSettings
{
    /// <summary>
    /// Default timeouts for tests and hooks.
    /// </summary>
    public static TimeoutSettings Timeouts { get; } = new();

    /// <summary>
    /// Controls concurrent test execution.
    /// </summary>
    public static ParallelismSettings Parallelism { get; } = new();

    /// <summary>
    /// Controls visual output.
    /// </summary>
    public static DisplaySettings Display { get; } = new();

    /// <summary>
    /// Controls test run behavior.
    /// </summary>
    public static ExecutionSettings Execution { get; } = new();
}
