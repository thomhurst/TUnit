namespace TUnit.Core.Settings;

/// <summary>
/// Programmatic configuration for TUnit. Access via <c>context.Settings</c> in a
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
public sealed class TUnitSettings
{
    internal static TUnitSettings Default { get; } = new();

    internal TUnitSettings() { }

    /// <summary>
    /// Default timeouts for tests and hooks.
    /// </summary>
    public TimeoutSettings Timeouts { get; } = new();

    /// <summary>
    /// Controls concurrent test execution.
    /// </summary>
    public ParallelismSettings Parallelism { get; } = new();

    /// <summary>
    /// Controls visual output.
    /// </summary>
    public DisplaySettings Display { get; } = new();

    /// <summary>
    /// Controls test run behavior.
    /// </summary>
    public ExecutionSettings Execution { get; } = new();
}
