namespace TUnit.Core;

/// <summary>
/// A TimeProvider that completes all delays instantly for fast test execution,
/// while still providing real system timestamps.
/// </summary>
public sealed class InstantTimeProvider : TimeProvider
{
    /// <summary>
    /// Singleton instance for reuse
    /// </summary>
    public static InstantTimeProvider Instance { get; } = new();

    /// <summary>
    /// Gets the current UTC time from the system clock
    /// </summary>
    public override DateTimeOffset GetUtcNow() => TimeProvider.System.GetUtcNow();

    /// <summary>
    /// Gets the system timezone
    /// </summary>
    public override TimeZoneInfo LocalTimeZone => TimeProvider.System.LocalTimeZone;

    /// <summary>
    /// Creates a timer that fires immediately, making delays complete instantly
    /// </summary>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        // Fire immediately instead of waiting for dueTime
        return new InstantTimer(callback, state);
    }

    /// <summary>
    /// Timer implementation that fires its callback immediately
    /// </summary>
    private sealed class InstantTimer : ITimer
    {
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private bool _disposed;

        public InstantTimer(TimerCallback callback, object? state)
        {
            _callback = callback;
            _state = state;

            // Fire the callback immediately on a thread pool thread to avoid blocking
            Task.Run(() =>
            {
                if (!_disposed)
                {
                    _callback(_state);
                }
            });
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            // No-op since we fire immediately
            return !_disposed;
        }

        public void Dispose()
        {
            _disposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}
