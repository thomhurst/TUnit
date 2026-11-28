using TUnit.Core.Logging;
#if NET
using System.Diagnostics;
using System.Runtime.Versioning;
#endif

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Resizes the parallel test limit while tests run. It samples machine load and test progress
/// on its own thread and applies what <see cref="AdaptiveParallelismPolicy"/> decides to the
/// <see cref="MaxParallelismLimiter"/>.
/// </summary>
internal sealed class AdaptiveParallelismController : IDisposable
{
#if NET
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMilliseconds(250);

    // Never disposed: the sampling thread may still be waiting on it after Dispose returns.
    private readonly ManualResetEventSlim _stopSignal = new(false);
    private readonly MaxParallelismLimiter _limiter;
    private readonly ILogger _logger;
    private Thread? _thread;

    // Timestamp of the queued probe work item, or 0 once a pool thread has run it.
    private long _probeQueuedAt;
    private long _lastProbeDelayTicks;

    // Guards the fields below, so no limit change lands after Dispose has reported the summary.
    private readonly Lock _stateLock = new();
    private int _lowestLimit;
    private int _highestLimit;
    private int _changes;
    private bool _stopped;

    private AdaptiveParallelismController(MaxParallelismLimiter limiter, ILogger logger)
    {
        _limiter = limiter;
        _logger = logger;
        _lowestLimit = _highestLimit = limiter.CurrentLimit;
    }
#endif

    /// <summary>
    /// Whether this runtime and operating system can be measured. Where they cannot, the
    /// limit must be resolved as fixed, because nothing would ever move an adaptive one.
    /// </summary>
    public static bool IsSupported
    {
        get
        {
#if NET
            // The systems MachineCpuMonitor can read machine-wide CPU on.
            return OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Starts adjusting the limit, or returns <c>null</c> when the limit is fixed.
    /// </summary>
    public static AdaptiveParallelismController? Start(MaxParallelismLimiter limiter, ILogger logger)
    {
#if NET
        if (!limiter.Limits.IsAdaptive)
        {
            return null;
        }

        // TestScheduler.GetParallelismLimits resolves a fixed limit on other systems.
        if (!(OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
        {
            return null;
        }

        var controller = new AdaptiveParallelismController(limiter, logger);

        // A dedicated thread: sampling must continue while every pool thread is busy or blocked,
        // which is exactly when the limit needs lowering.
        var thread = new Thread(controller.Run)
        {
            IsBackground = true,
            Name = "TUnit adaptive parallelism controller",
        };
        controller._thread = thread;
        thread.Start();
        return controller;
#else
        return null;
#endif
    }

#if NET
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private void Run()
    {
        var limits = _limiter.Limits;
        _logger.LogDebug($"Adaptive parallelism enabled: limit starts at {limits.Initial} and moves between {limits.Minimum} and {limits.Maximum}");

        try
        {
            Sample(limits);
        }
        catch (Exception ex)
        {
            // An exception escaping a dedicated thread terminates the process. Fall back to the
            // limit a run without adaptive parallelism uses.
            _logger.LogDebug($"Adaptive parallelism stopped after an error; limit reset to {limits.Initial}: {ex}");
            Apply(new AdaptiveDecision(limits.Initial, AdaptiveLimitChange.None), cpuUtilization: 0, memoryLoad: 0);
        }
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private void Sample(ParallelismLimits limits)
    {
#if NET9_0_OR_GREATER
        Func<TimeSpan> getProcessCpuTime = static () => Environment.CpuUsage.TotalTime;
#else
        // One Process instance for the controller's lifetime instead of one allocation per sample.
        using var process = Process.GetCurrentProcess();
        Func<TimeSpan> getProcessCpuTime = () =>
        {
            process.Refresh();
            return process.TotalProcessorTime;
        };
#endif
        using var machineCpu = MachineCpuMonitor.Create();
        machineCpu?.Sample();

        var policy = new AdaptiveParallelismPolicy(limits);
        var processorCount = Environment.ProcessorCount;
        var previousCpuTime = getProcessCpuTime();
        var previousTimestamp = Stopwatch.GetTimestamp();
        var previousReleasedSlots = _limiter.ReleasedSlots;
        var machineCpuUtilization = 0d;

        while (!_stopSignal.Wait(SampleInterval))
        {
            var cpuTime = getProcessCpuTime();
            var timestamp = Stopwatch.GetTimestamp();
            var releasedSlots = _limiter.ReleasedSlots;
            var elapsed = Stopwatch.GetElapsedTime(previousTimestamp, timestamp);

            var processCpu = elapsed <= TimeSpan.Zero
                ? 0
                : (cpuTime - previousCpuTime).TotalMilliseconds / (elapsed.TotalMilliseconds * processorCount);

            // The operating system does not advance its counters on every sample (macOS can
            // go a second without), so keep the last reading until the next one arrives.
            machineCpuUtilization = machineCpu?.Sample() ?? machineCpuUtilization;

            // Under a container CPU quota the process can be at its limit while the host is
            // mostly idle, so take the higher of the two.
            var cpuUtilization = Math.Min(1, Math.Max(processCpu, machineCpuUtilization));
            var memoryLoad = ReadMemoryLoad();

            var decision = policy.OnSample(new AdaptiveSample(
                elapsed,
                cpuUtilization,
                memoryLoad,
                SampleQueueDelay(timestamp),
                releasedSlots - previousReleasedSlots,
                LimitReached: _limiter.AvailableSlots == 0));

            if (decision.Change != AdaptiveLimitChange.None)
            {
                Apply(decision, cpuUtilization, memoryLoad);
            }

            previousCpuTime = cpuTime;
            previousTimestamp = timestamp;
            previousReleasedSlots = releasedSlots;
        }
    }

    private void Apply(AdaptiveDecision decision, double cpuUtilization, double memoryLoad)
    {
        int previousLimit;
        int limit;

        lock (_stateLock)
        {
            if (_stopped)
            {
                return;
            }

            previousLimit = _limiter.CurrentLimit;
            limit = _limiter.SetLimit(decision.Limit);

            if (limit == previousLimit)
            {
                return;
            }

            _lowestLimit = Math.Min(_lowestLimit, limit);
            _highestLimit = Math.Max(_highestLimit, limit);
            _changes++;
        }

        if (decision.Change != AdaptiveLimitChange.None && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"Adaptive parallelism: limit {previousLimit} -> {limit} ({Describe(decision.Change)}); CPU {cpuUtilization:P0}, memory load {memoryLoad:P0}");
        }
    }

    private static string Describe(AdaptiveLimitChange change) => change switch
    {
        AdaptiveLimitChange.Probe => "probing for more throughput",
        AdaptiveLimitChange.ProbeReverted => "the last raise did not improve throughput",
        AdaptiveLimitChange.Recovery => "pressure eased",
        AdaptiveLimitChange.MemoryPressure => "memory pressure",
        AdaptiveLimitChange.CpuSaturation => "CPUs saturated and work is queuing for a thread",
        _ => change.ToString(),
    };

    /// <summary>
    /// Memory in use relative to the level at which the runtime considers the machine (or the
    /// container, when it has a memory limit) short of memory.
    /// </summary>
    // The runtime refreshes these numbers at each garbage collection, and they are zero before
    // the first one. That is recent enough while tests allocate. Memory taken by child
    // processes or containers while this process collects rarely is seen late (#7873).
    internal static double ReadMemoryLoad()
    {
        var info = GC.GetGCMemoryInfo();
        return info.HighMemoryLoadThresholdBytes <= 0
            ? 0
            : (double) info.MemoryLoadBytes / info.HighMemoryLoadThresholdBytes;
    }

    /// <summary>
    /// How long the latest probe work item waited for a pool thread. A probe still waiting
    /// reports its wait so far, so the delay keeps growing while the pool is stuck.
    /// </summary>
    private TimeSpan SampleQueueDelay(long timestamp)
    {
        var probeQueuedAt = Volatile.Read(ref _probeQueuedAt);
        if (probeQueuedAt != 0)
        {
            return Stopwatch.GetElapsedTime(probeQueuedAt, timestamp);
        }

        var delay = Stopwatch.GetElapsedTime(0, Volatile.Read(ref _lastProbeDelayTicks));

        // The global queue is where timer callbacks and new work wait for a thread.
        Volatile.Write(ref _probeQueuedAt, timestamp);
        ThreadPool.UnsafeQueueUserWorkItem(static controller =>
        {
            var queuedAt = Volatile.Read(ref controller._probeQueuedAt);
            Volatile.Write(ref controller._lastProbeDelayTicks, Stopwatch.GetTimestamp() - queuedAt);
            Volatile.Write(ref controller._probeQueuedAt, 0);
        }, this, preferLocal: false);

        return delay;
    }
#endif

    public void Dispose()
    {
#if NET
        _stopSignal.Set();

        // The thread wakes on the signal and a sample takes microseconds, so this returns at
        // once. The timeout only guards against a sample stuck in the operating system.
        _thread?.Join(TimeSpan.FromSeconds(1));

        string summary;

        lock (_stateLock)
        {
            if (_stopped)
            {
                return;
            }

            // From here a sample that outlived the join can no longer change the limit.
            _stopped = true;

            summary = _changes == 0
                ? $"Adaptive parallelism: the parallel test limit stayed at {_limiter.CurrentLimit}"
                : $"Adaptive parallelism: the parallel test limit ranged from {_lowestLimit} to {_highestLimit} over {_changes} changes and ended at {_limiter.CurrentLimit}";
        }

        // Information, not Debug: a failure that depends on how many tests ran at once can
        // only be understood from a log that says how many did.
        _logger.LogInformation(summary);
#endif
    }
}
