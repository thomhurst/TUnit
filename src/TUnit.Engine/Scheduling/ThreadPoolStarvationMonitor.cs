using TUnit.Core.Logging;
#if NET
using System.Diagnostics;
using System.Runtime.Versioning;
using TUnit.Core.Settings;
using TUnit.Engine.Configuration;
#endif

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Raises the thread pool's minimum worker thread count while tests run, but only once the pool
/// is starved by blocked threads.
/// <para>
/// Test bodies run inline on pool threads. When concurrently running tests block (Thread.Sleep,
/// synchronous I/O, sync-over-async), the pool adds threads beyond its minimum (ProcessorCount by
/// default) at only one or two per second. Timer callbacks — Task.Delay, CancelAfter and TUnit's own
/// [Timeout] — also need a pool thread, so starvation makes timing-sensitive tests in other classes
/// overshoot their deadlines.
/// </para>
/// <para>
/// A static minimum is not free: extra pool threads spin while searching for work and slow down
/// suites of many short tests. The monitor therefore raises the minimum only after a probe work
/// item waits a whole sample interval for a thread while the process leaves most CPUs idle — the
/// signature of blocked, not busy, threads. Pending-item counts alone are not enough: the engine's
/// own short work items keep a small queue at low CPU without any test blocking.
/// </para>
/// </summary>
internal sealed class ThreadPoolStarvationMonitor : IDisposable
{
#if NET
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMilliseconds(50);

    // A probe queued on the previous sample has waited about one interval. The threshold sits
    // below the interval so that timer jitter in the wait cannot push detection back a sample.
    private static readonly TimeSpan StarvedProbeWait = TimeSpan.FromMilliseconds(40);

    // Below this share of total CPU capacity, queued work is waiting on blocked threads rather
    // than on busy ones, so adding threads helps instead of oversubscribing the cores.
    internal const double StarvedCpuUtilization = 0.5;

    // Upper bound on the raised minimum, per processor.
    internal const int MaxWorkerThreadsPerProcessor = 64;

    // Never disposed: the sampling thread may still be waiting on it after Dispose returns.
    private readonly ManualResetEventSlim _stopSignal = new(false);
    private readonly Lock _lock = new();
    private readonly ILogger _logger;
    // First raise target: the parallel test limit plus headroom.
    private readonly int _maxWorkerThreads;
    // Escalation stops here. Keyed [NotInParallel] tests and dynamic tests can run alongside the
    // limited phase, so more tests than the parallel limit can block at once.
    private readonly int _ceilingWorkerThreads;
    // The minimum just before the raise, so a value user code set after Start is the one restored.
    private int _minWorkerThreadsBeforeRaise;
    private int _raisedMinWorkerThreads;
    private bool _stopped;
    // Timestamp of the queued probe work item, or 0 once a pool thread has run it.
    private long _probeQueuedAt;

    private ThreadPoolStarvationMonitor(int maxWorkerThreads, int ceilingWorkerThreads, ILogger logger)
    {
        _maxWorkerThreads = maxWorkerThreads;
        _ceilingWorkerThreads = ceilingWorkerThreads;
        _logger = logger;
    }
#endif

    /// <summary>
    /// Starts monitoring, or returns <c>null</c> when monitoring cannot help or is disabled.
    /// </summary>
    public static ThreadPoolStarvationMonitor? Start(int maxParallelism, ILogger logger)
    {
#if NET
        // Browser and WASI are single-threaded, and iOS/tvOS do not support reading process CPU
        // time. Mobile and wasm suites rarely run enough blocking tests in parallel to need this.
        if (!(OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD()))
        {
            return null;
        }

        if (EnvironmentFlags.IsSet(EnvironmentConstants.DisableThreadPoolTuning))
        {
            logger.LogDebug($"Thread pool starvation monitor disabled by {EnvironmentConstants.DisableThreadPoolTuning}");
            return null;
        }

        if (!TUnitSettings.Default.Execution.ThreadPoolTuningEnabled)
        {
            logger.LogDebug("Thread pool starvation monitor disabled by TUnitSettings.Execution.ThreadPoolTuningEnabled");
            return null;
        }

        ThreadPool.GetMinThreads(out var originalMinWorkerThreads, out _);

        var maxWorkerThreads = ComputeMaxWorkerThreads(maxParallelism, Environment.ProcessorCount);

        if (originalMinWorkerThreads >= maxWorkerThreads)
        {
            // The user already configured enough threads.
            return null;
        }

        var monitor = new ThreadPoolStarvationMonitor(
            maxWorkerThreads,
            Math.Max(maxWorkerThreads, Environment.ProcessorCount * MaxWorkerThreadsPerProcessor),
            logger);

        // A dedicated thread: the monitor must keep sampling while every pool thread is blocked.
        var thread = new Thread(monitor.Run)
        {
            IsBackground = true,
            Name = "TUnit thread pool starvation monitor",
        };
        thread.Start();
        return monitor;
#else
        // ThreadPool.PendingWorkItemCount is unavailable; the runtime's own thread injection applies.
        return null;
#endif
    }

#if NET
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    private void Run()
    {
        try
        {
            Sample();
        }
        catch (Exception ex)
        {
            // An exception escaping a dedicated thread terminates the process. Monitoring is only
            // an optimization, so stop sampling and let the runtime's own thread injection apply.
            _logger.LogDebug($"Thread pool starvation monitor stopped after an error: {ex}");
        }
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    private void Sample()
    {
#if NET9_0_OR_GREATER
        Func<TimeSpan> getProcessCpuTime = static () => Environment.CpuUsage.TotalTime;
#else
        // One Process instance for the monitor's lifetime instead of one allocation per sample.
        using var process = Process.GetCurrentProcess();
        Func<TimeSpan> getProcessCpuTime = () =>
        {
            process.Refresh();
            return process.TotalProcessorTime;
        };
#endif
        var processorCount = Environment.ProcessorCount;
        var previousCpuTime = getProcessCpuTime();
        var previousTimestamp = Stopwatch.GetTimestamp();

        while (!_stopSignal.Wait(SampleInterval))
        {
            var cpuTime = getProcessCpuTime();
            var timestamp = Stopwatch.GetTimestamp();

            var elapsedMs = Stopwatch.GetElapsedTime(previousTimestamp, timestamp).TotalMilliseconds;
            var cpuUtilization = elapsedMs <= 0
                ? 1
                : (cpuTime - previousCpuTime).TotalMilliseconds / (elapsedMs * processorCount);

            var probeQueuedAt = Volatile.Read(ref _probeQueuedAt);

            if (probeQueuedAt == 0)
            {
                // The global queue is where timer callbacks wait for a thread, so a probe queued
                // there measures how late a Task.Delay or [Timeout] continuation would run.
                Volatile.Write(ref _probeQueuedAt, timestamp);
                ThreadPool.UnsafeQueueUserWorkItem(static monitor => Volatile.Write(ref monitor._probeQueuedAt, 0), this, preferLocal: false);
            }
            else if (IsStarved(Stopwatch.GetElapsedTime(probeQueuedAt, timestamp), cpuUtilization)
                && !TryRaise())
            {
                // At the ceiling, or user code took over the minimum: nothing left to do.
                return;
            }

            previousCpuTime = cpuTime;
            previousTimestamp = timestamp;
        }
    }

    /// <summary>
    /// Raises the minimum, or escalates an earlier raise that was not enough.
    /// Returns <c>false</c> when the monitor has nothing left to do.
    /// </summary>
    internal bool TryRaise()
    {
        lock (_lock)
        {
            if (_stopped)
            {
                return false;
            }

            ThreadPool.GetMinThreads(out var currentMinWorkerThreads, out var currentMinIoThreads);

            int target;
            if (_raisedMinWorkerThreads == 0)
            {
                // Raise straight to the target in one step. When threads are blocked in Task.Wait or
                // .Result, the runtime's cooperative-blocking logic takes over the raise and adds
                // threads only under narrow conditions. Repeated small raises then left the pool stuck
                // below its new minimum for seconds, but a single raise reliably adds the threads.
                _minWorkerThreadsBeforeRaise = currentMinWorkerThreads;
                target = _maxWorkerThreads;
            }
            else
            {
                if (currentMinWorkerThreads != _raisedMinWorkerThreads)
                {
                    // User code changed the minimum after the raise; leave it alone.
                    return false;
                }

                if (ThreadPool.ThreadCount < _raisedMinWorkerThreads)
                {
                    // The pool is still adding threads for the last raise. Raising again now would
                    // stack raises, which is what stalled the pool.
                    return true;
                }

                target = ComputeEscalatedMinimum(_raisedMinWorkerThreads, _ceilingWorkerThreads);
            }

            if (target <= currentMinWorkerThreads)
            {
                return false;
            }

            if (!ThreadPool.SetMinThreads(target, currentMinIoThreads))
            {
                return false;
            }

            _logger.LogDebug($"Thread pool starvation detected; raised minimum worker threads from {currentMinWorkerThreads} to {target}");
            _raisedMinWorkerThreads = target;
            return target < _ceilingWorkerThreads;
        }
    }

    /// <summary>
    /// Doubles a raise that did not end the starvation, up to the ceiling.
    /// </summary>
    internal static int ComputeEscalatedMinimum(int raisedMinWorkerThreads, int ceilingWorkerThreads)
        => (int)Math.Min((long)raisedMinWorkerThreads * 2, ceilingWorkerThreads);

    /// <summary>
    /// Queued work that waits about a whole sample interval for a thread while most CPUs are idle
    /// means pool threads are blocked rather than busy.
    /// </summary>
    internal static bool IsStarved(TimeSpan probeWait, double cpuUtilization)
        => probeWait >= StarvedProbeWait && cpuUtilization < StarvedCpuUtilization;

    /// <summary>
    /// The parallel test limit plus headroom for timer callbacks, the dynamic test queue and engine
    /// work, clamped so an oversized limit (for example <c>--maximum-parallel-tests 10000</c>) cannot
    /// turn off the runtime's thread injection throttling with a minimum of thousands of threads.
    /// </summary>
    internal static int ComputeMaxWorkerThreads(int maxParallelism, int processorCount)
        => (int)Math.Min((long)maxParallelism + processorCount, (long)processorCount * MaxWorkerThreadsPerProcessor);
#endif

    public void Dispose()
    {
#if NET
        lock (_lock)
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            _stopSignal.Set();

            if (_raisedMinWorkerThreads == 0)
            {
                return;
            }

            // Leave the pool as we found it unless user code changed the minimum in the meantime.
            ThreadPool.GetMinThreads(out var currentMinWorkerThreads, out var currentMinIoThreads);
            if (currentMinWorkerThreads != _raisedMinWorkerThreads)
            {
                return;
            }

            ThreadPool.SetMinThreads(_minWorkerThreadsBeforeRaise, currentMinIoThreads);
        }

        _logger.LogDebug($"Thread pool starvation monitor stopped; restored minimum worker threads to {_minWorkerThreadsBeforeRaise}");
#endif
    }
}
