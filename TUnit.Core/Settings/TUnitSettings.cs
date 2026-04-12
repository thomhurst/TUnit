namespace TUnit.Core.Settings;

/// <summary>
/// Programmatic configuration for TUnit. Set these in a
/// <c>[Before(HookType.TestDiscovery)]</c> hook to establish project-level defaults.
/// <para>
/// Precedence: CLI flag → environment variable → <see cref="TUnitSettings"/> → built-in default.
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
