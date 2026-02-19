namespace TUnit.Engine.Constants;

/// <summary>
/// Default configuration values for the TUnit test engine.
/// Centralizes magic numbers so they can be tuned in a single place.
/// </summary>
internal static class EngineDefaults
{
    // ── Discovery ───────────────────────────────────────────────────────

    /// <summary>
    /// Maximum time allowed for test discovery before the operation is cancelled.
    /// </summary>
    public static readonly TimeSpan DiscoveryTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum time allowed for test data generation in the discovery circuit breaker
    /// before it trips.
    /// </summary>
    public static readonly TimeSpan MaxGenerationTime = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Maximum proportion of available memory that test data generation may consume
    /// before the discovery circuit breaker trips.
    /// </summary>
    public const double MaxMemoryPercentage = 0.7;

    /// <summary>
    /// Conservative fallback for total available memory when the runtime cannot determine
    /// the actual value (e.g., on older .NET runtimes).
    /// </summary>
    public const long FallbackAvailableMemoryBytes = 1024L * 1024L * 1024L; // 1 GB

    /// <summary>
    /// Maximum number of retry passes when resolving test dependencies.
    /// </summary>
    public const int DependencyResolutionMaxRetries = 3;

    // ── Event Batching ──────────────────────────────────────────────────

    /// <summary>
    /// Default number of events collected before a batch is flushed.
    /// </summary>
    public const int DefaultEventBatchSize = 100;

    /// <summary>
    /// Minimum batch delay used when the caller specifies <see cref="TimeSpan.Zero"/>.
    /// Prevents a tight spin loop in the batching consumer.
    /// </summary>
    public static readonly TimeSpan MinBatchDelay = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// Maximum time to wait for the background processing task to complete
    /// during dispose / shutdown.
    /// </summary>
    public static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    // ── Timeout Handling ────────────────────────────────────────────────

    /// <summary>
    /// Grace period given to a timed-out task to handle cancellation
    /// before a <see cref="TimeoutException"/> is thrown.
    /// </summary>
    public static readonly TimeSpan TimeoutGracePeriod = TimeSpan.FromSeconds(1);

    // ── IDE Streaming ───────────────────────────────────────────────────

    /// <summary>
    /// Interval at which cumulative test output snapshots are streamed to the IDE.
    /// </summary>
    public static readonly TimeSpan IdeStreamingThrottleInterval = TimeSpan.FromSeconds(1);

    // ── File-Write Retry (Reporters) ────────────────────────────────────

    /// <summary>
    /// Maximum number of attempts when writing a report file that may be locked
    /// by another process.
    /// </summary>
    public const int FileWriteMaxAttempts = 5;

    /// <summary>
    /// Base delay in milliseconds for exponential back-off when retrying a locked file write.
    /// Actual delay = <c>BaseRetryDelayMs * 2^(attempt-1) + jitter</c>.
    /// </summary>
    public const int BaseRetryDelayMs = 50;

    /// <summary>
    /// Maximum random jitter in milliseconds added to retry delays to prevent thundering-herd effects.
    /// </summary>
    public const int MaxRetryJitterMs = 50;

    /// <summary>
    /// Maximum file size in bytes for the GitHub Step Summary file.
    /// GitHub imposes a 1 MB limit on step summary files.
    /// </summary>
    public const long GitHubSummaryMaxFileSizeBytes = 1L * 1024L * 1024L; // 1 MB
}
