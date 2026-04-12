using TUnit.Core.Settings;

namespace TUnit.Core;

/// <summary>
/// Default values shared across TUnit.Core and TUnit.Engine.
/// Centralizes magic numbers so they can be tuned in a single place.
/// </summary>
[Obsolete($"Use {nameof(TUnitSettings)}.{nameof(TUnitSettings.Timeouts)} instead.")]
public static class Defaults
{
    /// <summary>
    /// Default timeout applied to individual tests when no <c>[Timeout]</c> attribute is specified.
    /// Can be overridden per-test via <see cref="TUnit.Core.TimeoutAttribute"/>.
    /// </summary>
    [Obsolete($"Use {nameof(TUnitSettings)}.{nameof(TUnitSettings.Timeouts)}.{nameof(TimeoutSettings.DefaultTestTimeout)} instead.")]
    public static TimeSpan TestTimeout => TUnitSettings.Default.Timeouts.DefaultTestTimeout;

    /// <summary>
    /// Default timeout applied to hook methods (Before/After at every level)
    /// when no explicit timeout is configured.
    /// </summary>
    [Obsolete($"Use {nameof(TUnitSettings)}.{nameof(TUnitSettings.Timeouts)}.{nameof(TimeoutSettings.DefaultHookTimeout)} instead.")]
    public static TimeSpan HookTimeout => TUnitSettings.Default.Timeouts.DefaultHookTimeout;

    /// <summary>
    /// Time allowed for a graceful shutdown after a cancellation request (Ctrl+C / SIGTERM)
    /// before the process is forcefully terminated.
    /// </summary>
    [Obsolete($"Use {nameof(TUnitSettings)}.{nameof(TUnitSettings.Timeouts)}.{nameof(TimeoutSettings.ForcefulExitTimeout)} instead.")]
    public static TimeSpan ForcefulExitTimeout => TUnitSettings.Default.Timeouts.ForcefulExitTimeout;

    /// <summary>
    /// Brief delay during process exit to allow After hooks registered via
    /// <see cref="CancellationToken.Register"/> to execute before the process terminates.
    /// </summary>
    [Obsolete($"Use {nameof(TUnitSettings)}.{nameof(TUnitSettings.Timeouts)}.{nameof(TimeoutSettings.ProcessExitHookDelay)} instead.")]
    public static TimeSpan ProcessExitHookDelay => TUnitSettings.Default.Timeouts.ProcessExitHookDelay;
}
