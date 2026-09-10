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
        get => _defaultTestTimeout ?? TimeSpan.FromMinutes(30);
        set
        {
            ValidatePositive(value);
            _defaultTestTimeout = value;
        }
    }

    // Null means the user never assigned a project-level default, so tests without [Timeout]
    // bypass TimeoutHelper entirely instead of paying wrap overhead for a 30-minute backstop.
    internal TimeSpan? ExplicitDefaultTestTimeout => _defaultTestTimeout;

    // Coalesces the per-test [Timeout] attribute value with the project-wide opt-in
    // DefaultTestTimeout. Null when neither is set — TestCoordinator's null fast path
    // then skips the TimeoutHelper wrap entirely.
    internal TimeSpan? GetEffectiveTestTimeout(TimeSpan? attributeTimeout)
        => attributeTimeout ?? _defaultTestTimeout;

    // Test-only seam: the public setter validates positive-only and can't write null, which
    // leaves the harness unable to restore the "unset" state after a snapshot/restore cycle.
    internal void SetExplicitDefaultTestTimeout(TimeSpan? value)
    {
        if (value is { } positive)
        {
            ValidatePositive(positive);
        }

        _defaultTestTimeout = value;
    }

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

    private TimeSpan? _defaultTestTimeout;
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
