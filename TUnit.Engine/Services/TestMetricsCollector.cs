using System.Diagnostics;
using TUnit.Core.Logging;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

/// <summary>
/// Collects and reports test execution metrics when the --metrics flag is enabled.
/// Tracks test counts, durations, concurrency, and memory usage.
/// </summary>
internal sealed class TestMetricsCollector
{
    private readonly TUnitFrameworkLogger _logger;

    private int _totalTests;
    private int _passedTests;
    private int _failedTests;
    private int _skippedTests;

    private int _currentConcurrentTests;
    private int _peakConcurrentTests;

    private long _totalTestDurationTicks;
    private int _completedTestsWithDuration;

    private long _memoryAtStart;
    private long _memoryAtEnd;

    private readonly Stopwatch _wallClockStopwatch = new();

    public TestMetricsCollector(TUnitFrameworkLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called once before test execution begins to capture initial state.
    /// </summary>
    public void OnExecutionStarted(int totalTestCount)
    {
        _totalTests = totalTestCount;
        _memoryAtStart = GC.GetTotalMemory(forceFullCollection: false);
        _wallClockStopwatch.Start();
    }

    /// <summary>
    /// Called when an individual test begins executing.
    /// </summary>
    public void OnTestStarted()
    {
        var current = Interlocked.Increment(ref _currentConcurrentTests);

        // Update peak using a lock-free CAS loop
        int peak;
        do
        {
            peak = Volatile.Read(ref _peakConcurrentTests);
            if (current <= peak)
            {
                break;
            }
        } while (Interlocked.CompareExchange(ref _peakConcurrentTests, current, peak) != peak);
    }

    /// <summary>
    /// Called when an individual test finishes executing.
    /// </summary>
    public void OnTestCompleted(bool passed, bool skipped, TimeSpan? duration)
    {
        Interlocked.Decrement(ref _currentConcurrentTests);

        if (skipped)
        {
            Interlocked.Increment(ref _skippedTests);
        }
        else if (passed)
        {
            Interlocked.Increment(ref _passedTests);
        }
        else
        {
            Interlocked.Increment(ref _failedTests);
        }

        if (duration.HasValue)
        {
            Interlocked.Add(ref _totalTestDurationTicks, duration.Value.Ticks);
            Interlocked.Increment(ref _completedTestsWithDuration);
        }
    }

    /// <summary>
    /// Called after all tests have finished. Captures final state and logs the summary.
    /// </summary>
    public async ValueTask OnExecutionFinishedAsync()
    {
        _wallClockStopwatch.Stop();
        _memoryAtEnd = GC.GetTotalMemory(forceFullCollection: false);

        var summary = BuildSummary();
        await _logger.LogAsync(LogLevel.Information, summary, null, static (s, _) => s);
    }

    private string BuildSummary()
    {
        var wallClock = _wallClockStopwatch.Elapsed;
        var completedWithDuration = Volatile.Read(ref _completedTestsWithDuration);
        var avgDuration = completedWithDuration > 0
            ? TimeSpan.FromTicks(Volatile.Read(ref _totalTestDurationTicks) / completedWithDuration)
            : TimeSpan.Zero;

        var memStartMb = _memoryAtStart / (1024.0 * 1024.0);
        var memEndMb = _memoryAtEnd / (1024.0 * 1024.0);
        var memDeltaMb = memEndMb - memStartMb;
        var sign = memDeltaMb >= 0 ? "+" : "";

        return $"""

            --- Test Execution Metrics ---
            Total tests:            {Volatile.Read(ref _totalTests)}
            Passed:                 {Volatile.Read(ref _passedTests)}
            Failed:                 {Volatile.Read(ref _failedTests)}
            Skipped:                {Volatile.Read(ref _skippedTests)}
            Average test duration:  {FormatDuration(avgDuration)}
            Peak concurrent tests:  {Volatile.Read(ref _peakConcurrentTests)}
            Wall-clock time:        {FormatDuration(wallClock)}
            Memory at start:        {memStartMb:F1} MB
            Memory at end:          {memEndMb:F1} MB
            Memory delta:           {sign}{memDeltaMb:F1} MB
            ------------------------------
            """;
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalMilliseconds < 1)
        {
            var microseconds = ts.Ticks / 10.0;
            return $"{microseconds:F0} us";
        }

        if (ts.TotalSeconds < 1)
        {
            return $"{ts.TotalMilliseconds:F1} ms";
        }

        if (ts.TotalMinutes < 1)
        {
            return $"{ts.TotalSeconds:F2} s";
        }

        return $"{ts.TotalMinutes:F1} min";
    }
}
