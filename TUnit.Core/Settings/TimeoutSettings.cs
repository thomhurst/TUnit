namespace TUnit.Core.Settings;

/// <summary>
/// Default timeouts applied when no <c>[Timeout]</c> attribute is specified.
/// These are project-level defaults — CLI flags and environment variables take precedence.
/// </summary>
public sealed class TimeoutSettings
{
    /// <summary>
    /// Default timeout for individual tests. Default: 30 minutes.
    /// Overridden per-test by <see cref="TimeoutAttribute"/>.
    /// Precedence: CLI/env var (N/A for test timeout) → TUnitSettings → built-in default.
    /// </summary>
    public TimeSpan DefaultTestTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Default timeout for hook methods (Before/After at every level). Default: 5 minutes.
    /// Overridden per-hook by <see cref="TimeoutAttribute"/>.
    /// <para>
    /// <b>Note:</b> Hook methods capture this value at registration time, which occurs before
    /// <c>[Before(HookType.TestDiscovery)]</c> hooks run. Use the <c>[Timeout]</c> attribute
    /// on individual hook methods for reliable per-hook timeout control.
    /// </para>
    /// </summary>
    public TimeSpan DefaultHookTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Time allowed for graceful shutdown after cancellation (Ctrl+C / SIGTERM)
    /// before the process is forcefully terminated. Default: 30 seconds.
    /// </summary>
    public TimeSpan ForcefulExitTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Brief delay during process exit to allow After hooks registered via
    /// <see cref="CancellationToken.Register"/> to execute. Default: 500ms.
    /// </summary>
    public TimeSpan ProcessExitHookDelay { get; set; } = TimeSpan.FromMilliseconds(500);
}
