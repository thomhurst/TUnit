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
    public static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Default timeout applied to hook methods (Before/After at every level)
    /// when no explicit timeout is configured.
    /// </summary>
    [Obsolete($"Use {nameof(TUnitSettings)}.{nameof(TUnitSettings.Timeouts)}.{nameof(TimeoutSettings.DefaultHookTimeout)} instead.")]
    public static readonly TimeSpan HookTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Time allowed for a graceful shutdown after a cancellation request (Ctrl+C / SIGTERM)
    /// before the process is forcefully terminated.
    /// </summary>
    [Obsolete($"Use {nameof(TUnitSettings)}.{nameof(TUnitSettings.Timeouts)}.{nameof(TimeoutSettings.ForcefulExitTimeout)} instead.")]
    public static readonly TimeSpan ForcefulExitTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Brief delay during process exit to allow After hooks registered via
    /// <see cref="CancellationToken.Register"/> to execute before the process terminates.
    /// </summary>
    [Obsolete($"Use {nameof(TUnitSettings)}.{nameof(TUnitSettings.Timeouts)}.{nameof(TimeoutSettings.ProcessExitHookDelay)} instead.")]
    public static readonly TimeSpan ProcessExitHookDelay = TimeSpan.FromMilliseconds(500);
}
