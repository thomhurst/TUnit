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

    // Below this share of total CPU capacity, queued work is waiting on blocked threads rather
    // than on busy ones, so adding threads helps instead of oversubscribing the cores.
    internal const double StarvedCpuUtilization = 0.5;

    // Never disposed: the sampling thread may still be waiting on it after Dispose returns.
    private readonly ManualResetEventSlim _stopSignal = new(false);
    private readonly Lock _lock = new();
    private readonly ILogger _logger;
    private readonly int _originalMinWorkerThreads;
    private readonly int _maxWorkerThreads;
    private int _raisedMinWorkerThreads;
    private bool _stopped;
    // Timestamp of the queued probe work item, or 0 once a pool thread has run it.
    private long _probeQueuedAt;

    private ThreadPoolStarvationMonitor(int originalMinWorkerThreads, int maxWorkerThreads, ILogger logger)
    {
        _originalMinWorkerThreads = originalMinWorkerThreads;
        _maxWorkerThreads = maxWorkerThreads;
        _logger = logger;
    }
#endif

    /// <summary>
    /// Starts monitoring, or returns <c>null</c> when monitoring cannot help or is disabled.
    /// </summary>
    public static ThreadPoolStarvationMonitor? Start(int maxParallelism, ILogger logger)
    {
#if NET
        if (OperatingSystem.IsBrowser())
        {
            return null;
        }

        if (IsTruthyEnv(Environment.GetEnvironmentVariable(EnvironmentConstants.DisableThreadPoolTuning)))
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

        // Headroom above the test limit for timer callbacks, the dynamic test queue and engine work.
        var maxWorkerThreads = (int)Math.Min((long)maxParallelism + Environment.ProcessorCount, short.MaxValue);

        if (originalMinWorkerThreads >= maxWorkerThreads)
        {
            // The user already configured enough threads.
            return null;
        }

        var monitor = new ThreadPoolStarvationMonitor(originalMinWorkerThreads, maxWorkerThreads, logger);

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
    [UnsupportedOSPlatform("browser")]
    private void Run()
    {
        var processorCount = Environment.ProcessorCount;
        var previousCpuTime = GetProcessCpuTime();
        var previousTimestamp = Stopwatch.GetTimestamp();

        while (!_stopSignal.Wait(SampleInterval))
        {
            var cpuTime = GetProcessCpuTime();
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
            else if (IsStarved(Stopwatch.GetElapsedTime(probeQueuedAt, timestamp), cpuUtilization))
            {
                // One raise to the cap is all the monitor does, so it stops sampling afterwards.
                Raise();
                return;
            }

            previousCpuTime = cpuTime;
            previousTimestamp = timestamp;
        }
    }

    private void Raise()
    {
        lock (_lock)
        {
            if (_stopped)
            {
                return;
            }

            // Raise straight to the cap in one step. When threads are blocked in Task.Wait or .Result,
            // the runtime's cooperative-blocking logic takes over the raise and adds threads only
            // under narrow conditions. Repeated small raises then left the pool stuck below its new
            // minimum for seconds, but a single raise reliably adds the threads.
            ThreadPool.GetMinThreads(out var currentMinWorkerThreads, out var currentMinIoThreads);

            if (currentMinWorkerThreads < _maxWorkerThreads
                && ThreadPool.SetMinThreads(_maxWorkerThreads, currentMinIoThreads))
            {
                _raisedMinWorkerThreads = _maxWorkerThreads;
            }
        }
    }

    [UnsupportedOSPlatform("browser")]
    private static TimeSpan GetProcessCpuTime()
    {
#if NET9_0_OR_GREATER
        return Environment.CpuUsage.TotalTime;
#else
        using var process = Process.GetCurrentProcess();
        return process.TotalProcessorTime;
#endif
    }

    /// <summary>
    /// Queued work that waits a whole sample interval for a thread while most CPUs are idle means
    /// pool threads are blocked rather than busy.
    /// </summary>
    internal static bool IsStarved(TimeSpan probeWait, double cpuUtilization)
        => probeWait >= SampleInterval && cpuUtilization < StarvedCpuUtilization;

    private static bool IsTruthyEnv(string? value)
        => value is not null
           && (value == "1"
               || value.Equals("true", StringComparison.OrdinalIgnoreCase)
               || value.Equals("yes", StringComparison.OrdinalIgnoreCase));
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
            if (currentMinWorkerThreads == _raisedMinWorkerThreads)
            {
                ThreadPool.SetMinThreads(_originalMinWorkerThreads, currentMinIoThreads);
            }
        }

        _logger.LogDebug($"Thread pool starvation detected while tests ran; minimum worker threads were raised from {_originalMinWorkerThreads} to {_raisedMinWorkerThreads}");
#endif
    }
}
