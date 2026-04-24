namespace TUnit.Core.Settings;

/// <summary>
/// Default timeouts applied when no <c>[Timeout]</c> attribute is specified.
/// These are project-level defaults — CLI flags and environment variables take precedence.
/// </summary>
public sealed class TimeoutSettings
{
    internal TimeoutSettings() { }

    /// <summary>
    /// Default timeout for individual tests. Overridden per-test by <see cref="TimeoutAttribute"/>.
    /// Precedence: CLI/env var (N/A for test timeout) → TUnitSettings → built-in default.
    /// <para>
    /// If this property is never explicitly set, tests without a <see cref="TimeoutAttribute"/>
    /// run without any timeout wrapper. The 30-minute fallback only applies when this
    /// property is explicitly assigned.
    /// </para>
    /// </summary>
    public TimeSpan DefaultTestTimeout
    {
        get => _defaultTestTimeout;
        set
        {
            ValidatePositive(value);
            _defaultTestTimeout = value;
            _defaultTestTimeoutExplicitlySet = true;
        }
    }

    // When false, tests without [Timeout] bypass TimeoutHelper entirely —
    // applying the 30-minute built-in default to every test would be pure overhead.
    internal bool DefaultTestTimeoutExplicitlySet => _defaultTestTimeoutExplicitlySet;

    /// <summary>
    /// Default timeout for hook methods (Before/After at every level). Default: 5 minutes.
    /// Overridden per-hook by <see cref="TimeoutAttribute"/>.
    /// </summary>
    public TimeSpan DefaultHookTimeout
    {
        get => _defaultHookTimeout;
        set
        {
            ValidatePositive(value);
            _defaultHookTimeout = value;
        }
    }

    /// <summary>
    /// Time allowed for graceful shutdown after cancellation (Ctrl+C / SIGTERM)
    /// before the process is forcefully terminated. Default: 30 seconds.
    /// </summary>
    public TimeSpan ForcefulExitTimeout
    {
        get => _forcefulExitTimeout;
        set
        {
            ValidatePositive(value);
            _forcefulExitTimeout = value;
        }
    }

    /// <summary>
    /// Brief delay during process exit to allow After hooks registered via
    /// <see cref="CancellationToken.Register"/> to execute. Default: 500ms.
    /// Set to <see cref="TimeSpan.Zero"/> to disable the delay.
    /// </summary>
    public TimeSpan ProcessExitHookDelay
    {
        get => _processExitHookDelay;
        set
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "ProcessExitHookDelay cannot be negative.");
            }

            _processExitHookDelay = value;
        }
    }

    private TimeSpan _defaultTestTimeout = TimeSpan.FromMinutes(30);
    private bool _defaultTestTimeoutExplicitlySet;
    private TimeSpan _defaultHookTimeout = TimeSpan.FromMinutes(5);
    private TimeSpan _forcefulExitTimeout = TimeSpan.FromSeconds(30);
    private TimeSpan _processExitHookDelay = TimeSpan.FromMilliseconds(500);

    private static void ValidatePositive(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value,
                "Timeout must be a positive duration.");
        }
    }
}
