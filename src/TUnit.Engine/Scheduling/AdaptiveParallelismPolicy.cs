namespace TUnit.Engine.Scheduling;

/// <summary>
/// One observation of the machine and of test progress since the previous one.
/// </summary>
/// <param name="Elapsed">Time since the previous sample.</param>
/// <param name="CpuUtilization">Share of the machine's CPU capacity in use, 0 to 1.</param>
/// <param name="MemoryLoad">Memory in use relative to the level the runtime treats as high. 1 or more means memory pressure.</param>
/// <param name="QueueDelay">How long work queued to the thread pool waited for a thread.</param>
/// <param name="CompletedTests">Tests that finished since the previous sample.</param>
/// <param name="LimitReached">Whether every slot of the parallel limit was in use.</param>
internal readonly record struct AdaptiveSample(
    TimeSpan Elapsed,
    double CpuUtilization,
    double MemoryLoad,
    TimeSpan QueueDelay,
    long CompletedTests,
    bool LimitReached);

internal enum AdaptiveLimitChange
{
    None,

    /// <summary>Raised to find out whether more concurrent tests complete more tests per second.</summary>
    Probe,

    /// <summary>The last probe did not raise throughput enough, so its raise was undone.</summary>
    ProbeReverted,

    /// <summary>Raised back toward the starting limit after pressure eased.</summary>
    Recovery,

    /// <summary>Lowered because memory use reached the runtime's high-memory level.</summary>
    MemoryPressure,

    /// <summary>Lowered because the CPUs were saturated while work queued for a thread.</summary>
    CpuSaturation,
}

internal readonly record struct AdaptiveDecision(int Limit, AdaptiveLimitChange Change);

/// <summary>
/// Decides the parallel test limit from a stream of <see cref="AdaptiveSample"/>s. It holds no
/// clock and does no I/O, so the same samples always produce the same decisions.
/// </summary>
/// <remarks>
/// <para>
/// Machine headroom alone cannot justify a higher limit: tests that wait on a database pool, a
/// container or a lock leave the CPU idle, and running more of them only makes each one wait
/// longer inside its body, toward [Timeout]. So a raise is a probe. The policy measures completed
/// tests per second, raises the limit, measures again, and keeps the raise only when throughput
/// rose by a fair share of the raise. CPU, memory and thread pool delay are guards: they stop
/// probes and lower the limit, but never raise it on their own.
/// </para>
/// <para>
/// The starting limit is the one exception. It is the fixed limit a run without adaptive
/// parallelism uses, so the policy returns to it without proof once pressure has eased.
/// </para>
/// </remarks>
internal sealed class AdaptiveParallelismPolicy
{
    // At or above this, with work also queuing for a thread, more runnable work exists than CPUs.
    internal const double SaturatedCpuUtilization = 0.95;

    // A probe needs room to show a gain. Above these the machine is already close to a limit.
    internal const double CpuHeadroomUtilization = 0.8;
    internal const double MemoryHeadroomLoad = 0.85;

    internal const double MemoryPressureLoad = 1.0;

    // Longer than OS scheduling jitter, shorter than a delay a timing-sensitive test would notice.
    internal static readonly TimeSpan QueueDelayLimit = TimeSpan.FromMilliseconds(100);

    // One saturated sample can be a burst (a JIT spike, a GC). Two in a row is sustained.
    internal const int SaturatedSamplesBeforeDecrease = 2;

    // Memory pressure risks swapping or an out-of-memory kill, so it halves the limit. CPU
    // saturation only slows tests down, so it takes a quarter.
    internal const double MemoryDecreaseFactor = 0.5;
    internal const double CpuDecreaseFactor = 0.75;

    // Large enough that its effect on throughput stands out from window-to-window noise.
    internal const double IncreaseFactor = 1.5;

    // A probe is kept when throughput rose by at least this share of the raise: a 50% raise
    // must bring 15% more completed tests per second.
    internal const double MinimumGainShare = 0.3;

    internal static readonly TimeSpan MinimumWindow = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan MaximumWindow = TimeSpan.FromSeconds(30);

    // Fewer completed tests than this in a whole window is too few to compare throughput.
    internal const int MinimumWindowCompletions = 8;

    // A lower limit takes effect only as running tests finish, so wait before lowering again.
    internal static readonly TimeSpan PressureCooldown = TimeSpan.FromSeconds(3);

    // After a failed probe or pressure, how long before the next probe. Doubles each time.
    internal static readonly TimeSpan InitialHold = TimeSpan.FromSeconds(15);
    internal static readonly TimeSpan MaximumHold = TimeSpan.FromMinutes(2);

    // After a change, tests admitted under the old limit are still running. Throughput reflects
    // the new limit only once about one test duration has passed.
    internal static readonly TimeSpan MaximumSettle = TimeSpan.FromSeconds(15);

    private readonly ParallelismLimits _limits;

    private int _limit;
    private TimeSpan _clock;

    private bool _settling;
    private TimeSpan _settleRemaining;

    // Set while a probe is waiting for its verdict.
    private bool _probing;
    private int _limitBeforeProbe;
    private double _throughputBeforeProbe;

    // The limit divided by throughput: the average time a test holds a slot.
    private TimeSpan _averageSlotTime = MinimumWindow;

    private TimeSpan _hold = InitialHold;
    private TimeSpan _holdUntil;

    // While pressure is recent, the limit stays below the one that caused it.
    private int _pressureCap = int.MaxValue;
    private TimeSpan _pressureCapUntil;
    private TimeSpan _pressureCooldownUntil;
    private int _saturatedSamples;

    private TimeSpan _windowElapsed;
    private long _windowCompleted;
    private double _windowCpuSeconds;
    private double _windowMaxMemoryLoad;
    private TimeSpan _windowMaxQueueDelay;
    private int _windowSamples;
    private int _windowLimitReachedSamples;

    public AdaptiveParallelismPolicy(ParallelismLimits limits)
    {
        _limits = limits;
        _limit = limits.Initial;
    }

    public int Limit => _limit;

    public AdaptiveDecision OnSample(AdaptiveSample sample)
    {
        _clock += sample.Elapsed;

        if (TryRelievePressure(sample) is { } relieved)
        {
            return relieved;
        }

        if (_settling)
        {
            _settleRemaining -= sample.Elapsed;
            if (_settleRemaining <= TimeSpan.Zero)
            {
                _settling = false;
                ResetWindow();
            }

            return NoChange();
        }

        Accumulate(sample);

        if (!IsWindowComplete())
        {
            return NoChange();
        }

        var throughput = _windowCompleted / _windowElapsed.TotalSeconds;

        // Throughput is comparable between windows only when the limit, not a shortage of
        // tests ready to run, decided how many ran.
        var measurable = _windowCompleted >= MinimumWindowCompletions
            && _windowLimitReachedSamples * 2 >= _windowSamples;

        var hasHeadroom = _windowCpuSeconds / _windowElapsed.TotalSeconds < CpuHeadroomUtilization
            && _windowMaxMemoryLoad < MemoryHeadroomLoad
            && _windowMaxQueueDelay < QueueDelayLimit;

        ResetWindow();

        if (measurable)
        {
            _averageSlotTime = TimeSpan.FromSeconds(_limit / throughput);
        }

        if (_probing)
        {
            _probing = false;

            var raise = (double) _limit / _limitBeforeProbe - 1;
            var requiredThroughput = _throughputBeforeProbe * (1 + raise * MinimumGainShare);

            if (!measurable || throughput < requiredThroughput)
            {
                HoldProbes();
                return ChangeLimit(_limitBeforeProbe, AdaptiveLimitChange.ProbeReverted);
            }

            _hold = InitialHold;
        }

        if (!hasHeadroom)
        {
            return NoChange();
        }

        var ceiling = _clock < _pressureCapUntil
            ? Math.Min(_limits.Maximum, _pressureCap)
            : _limits.Maximum;

        if (_limit >= ceiling)
        {
            return NoChange();
        }

        var raised = Math.Min(ceiling, Math.Max(_limit + 1, (int) Math.Ceiling(_limit * IncreaseFactor)));

        if (_limit < _limits.Initial)
        {
            return ChangeLimit(Math.Min(raised, _limits.Initial), AdaptiveLimitChange.Recovery);
        }

        if (!measurable || _clock < _holdUntil)
        {
            return NoChange();
        }

        _probing = true;
        _limitBeforeProbe = _limit;
        _throughputBeforeProbe = throughput;
        return ChangeLimit(raised, AdaptiveLimitChange.Probe);
    }

    private AdaptiveDecision? TryRelievePressure(AdaptiveSample sample)
    {
        var saturated = sample.CpuUtilization >= SaturatedCpuUtilization && sample.QueueDelay >= QueueDelayLimit;
        _saturatedSamples = saturated ? _saturatedSamples + 1 : 0;

        if (_limit <= _limits.Minimum || _clock < _pressureCooldownUntil)
        {
            return null;
        }

        if (sample.MemoryLoad >= MemoryPressureLoad)
        {
            return Lower(MemoryDecreaseFactor, AdaptiveLimitChange.MemoryPressure);
        }

        if (_saturatedSamples >= SaturatedSamplesBeforeDecrease)
        {
            return Lower(CpuDecreaseFactor, AdaptiveLimitChange.CpuSaturation);
        }

        return null;
    }

    private AdaptiveDecision Lower(double factor, AdaptiveLimitChange change)
    {
        var lowered = Math.Max(_limits.Minimum, Math.Min(_limit - 1, (int) (_limit * factor)));

        // Halfway back is the highest the limit may return to while the pressure is recent, so
        // repeated pressure closes in on a limit the machine can hold.
        _pressureCap = Math.Max(lowered, (_limit + lowered) / 2);
        _pressureCapUntil = _clock + _hold;
        _pressureCooldownUntil = _clock + PressureCooldown;
        _saturatedSamples = 0;
        _probing = false;
        HoldProbes();

        return ChangeLimit(lowered, change);
    }

    private void HoldProbes()
    {
        _holdUntil = _clock + _hold;
        _hold = TimeSpan.FromTicks(Math.Min(_hold.Ticks * 2, MaximumHold.Ticks));
    }

    private AdaptiveDecision ChangeLimit(int limit, AdaptiveLimitChange change)
    {
        _limit = limit;
        _settling = true;
        _settleRemaining = TimeSpan.FromTicks(Math.Min(Math.Max(_averageSlotTime.Ticks, MinimumWindow.Ticks), MaximumSettle.Ticks));
        return new AdaptiveDecision(limit, change);
    }

    private AdaptiveDecision NoChange() => new(_limit, AdaptiveLimitChange.None);

    private void Accumulate(AdaptiveSample sample)
    {
        _windowElapsed += sample.Elapsed;
        _windowCompleted += sample.CompletedTests;
        _windowCpuSeconds += sample.CpuUtilization * sample.Elapsed.TotalSeconds;
        _windowMaxMemoryLoad = Math.Max(_windowMaxMemoryLoad, sample.MemoryLoad);
        _windowMaxQueueDelay = sample.QueueDelay > _windowMaxQueueDelay ? sample.QueueDelay : _windowMaxQueueDelay;
        _windowSamples++;

        if (sample.LimitReached)
        {
            _windowLimitReachedSamples++;
        }
    }

    // Long enough to average over at least two tests per slot, so a mix of short and long
    // tests does not read as a change in throughput. Tests too long for that close the window
    // at the maximum instead.
    private bool IsWindowComplete()
        => _windowElapsed >= MaximumWindow
           || (_windowElapsed >= MinimumWindow && _windowCompleted >= Math.Max(MinimumWindowCompletions, _limit * 2L));

    private void ResetWindow()
    {
        _windowElapsed = TimeSpan.Zero;
        _windowCompleted = 0;
        _windowCpuSeconds = 0;
        _windowMaxMemoryLoad = 0;
        _windowMaxQueueDelay = TimeSpan.Zero;
        _windowSamples = 0;
        _windowLimitReachedSamples = 0;
    }
}
