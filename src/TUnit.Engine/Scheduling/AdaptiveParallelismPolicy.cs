namespace TUnit.Engine.Scheduling;

/// <summary>
/// One observation of the machine and of test progress since the previous one.
/// </summary>
/// <param name="Elapsed">Time since the previous sample.</param>
/// <param name="CpuUtilization">Share of the machine's CPU capacity in use, 0 to 1.</param>
/// <param name="MemoryLoad">Memory in use relative to the level that counts as high. 1 or more means memory pressure.</param>
/// <param name="QueueDelay">How long work queued to the thread pool waited for a thread.</param>
/// <param name="CpuStalled">Whether the operating system reported tasks waiting for a CPU, or a CPU quota throttling the process.</param>
/// <param name="CompletedTests">Tests that finished since the previous sample.</param>
/// <param name="LimitReached">Whether every slot of the parallel limit was in use.</param>
internal readonly record struct AdaptiveSample(
    TimeSpan Elapsed,
    double CpuUtilization,
    double MemoryLoad,
    TimeSpan QueueDelay,
    long CompletedTests,
    bool LimitReached,
    bool CpuStalled = false);

internal enum AdaptiveLimitChange
{
    None,

    /// <summary>Raised to find out whether more concurrent tests complete more tests per second.</summary>
    Probe,

    /// <summary>The last probe did not raise throughput enough, so its raise was undone.</summary>
    ProbeReverted,

    /// <summary>Raised back toward the starting limit after pressure eased.</summary>
    Recovery,

    /// <summary>Lowered because the machine or container is nearly out of memory.</summary>
    MemoryPressure,

    /// <summary>Lowered to find out whether fewer concurrent tests relieve saturated CPUs at no cost in throughput.</summary>
    CpuSaturation,

    /// <summary>The last lowering for CPU saturation cost throughput, so it was undone.</summary>
    LowerReverted,
}

internal readonly record struct AdaptiveDecision(int Limit, AdaptiveLimitChange Change);

/// <summary>
/// Decides the parallel test limit from a stream of <see cref="AdaptiveSample"/>s. It holds no
/// clock and does no I/O, so the same samples always produce the same decisions.
/// </summary>
/// <remarks>
/// <para>
/// The run starts at the limit a run without adaptive parallelism uses, and a move away from it
/// has to pay for itself in completed tests per second. Machine metrics alone cannot justify
/// one. Idle CPUs do not mean more tests would help: tests that wait on a database pool, a
/// container or a lock leave the CPU idle, and running more of them only makes each one wait
/// longer inside its body, toward [Timeout]. Saturated CPUs do not mean fewer tests would help
/// either: when other processes cause the saturation, or the tests mostly wait on I/O, a lower
/// limit only gives throughput away.
/// </para>
/// <para>
/// So every change is a trial. The policy measures throughput, changes the limit, measures
/// again, and keeps a raise only when throughput rose by a fair share of the raise, and a
/// lowering only when throughput fell by less than a fair share of it. A failed trial puts the
/// limit back and makes the next one wait longer.
/// </para>
/// <para>
/// There are two exceptions. Memory pressure lowers the limit at once, because swapping or an
/// out-of-memory kill costs far more than a trial. And the starting limit needs no proof: the
/// policy returns to it once the machine has headroom again.
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

    // Memory pressure risks swapping or an out-of-memory kill, so it halves the limit at once.
    // CPU saturation only slows tests down, so it takes a quarter, on trial.
    internal const double MemoryDecreaseFactor = 0.5;
    internal const double CpuDecreaseFactor = 0.75;

    // Large enough that its effect on throughput stands out from window-to-window noise.
    internal const double IncreaseFactor = 1.5;

    // A raise is kept when throughput rose by at least this share of it: a 50% raise must
    // bring 15% more completed tests per second. A lowering is kept when throughput fell by
    // less than this share of it: a 25% lowering may cost at most 7.5%. Measurement noise then
    // sends an undecided trial back to the limit it started from.
    internal const double MinimumGainShare = 0.3;

    internal static readonly TimeSpan MinimumWindow = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan MaximumWindow = TimeSpan.FromSeconds(30);

    // Fewer completed tests than this in a whole window is too few to compare throughput.
    internal const int MinimumWindowCompletions = 8;

    // A lower limit takes effect only as running tests finish, so wait before lowering again.
    internal static readonly TimeSpan PressureCooldown = TimeSpan.FromSeconds(3);

    // After a failed trial or memory pressure, how long before the next trial. Doubles each time.
    internal static readonly TimeSpan InitialHold = TimeSpan.FromSeconds(15);
    internal static readonly TimeSpan MaximumHold = TimeSpan.FromMinutes(2);

    // After a change, tests admitted under the old limit are still running. Throughput reflects
    // the new limit only once about one test duration has passed.
    internal static readonly TimeSpan MaximumSettle = TimeSpan.FromSeconds(15);

    private readonly ParallelismLimits _limits;

    // Lowering for CPU saturation stops here. With no more tests running than there are
    // processors, this process is not what oversubscribes them, and going lower would only
    // leave processors unused or hand them to other processes.
    private readonly int _cpuSaturationFloor;

    private int _limit;
    private TimeSpan _clock;

    private bool _settling;
    private TimeSpan _settleRemaining;

    // Set while a raise or a lowering is waiting for its verdict.
    private AdaptiveLimitChange _trial;
    private int _limitBeforeTrial;
    private double _throughputBeforeTrial;

    // Throughput of the latest window, and the limit it was measured at: 0 when that window
    // could not be compared with another. A trial starts only from a measurement at the
    // current limit.
    private double _measuredThroughput;
    private int _measuredLimit;

    // Throughput before the first of a run of lowerings, and the limit it was measured at.
    // Every lowering is judged against it for as long as the limit stays below that one, so
    // neither several steps down nor a retry after a rejected step can each give a little
    // throughput away. It lapses once the limit is back where the run began.
    private double _throughputBeforeLowering;
    private int _limitBeforeLowering;

    // The limit divided by throughput: the average time a test holds a slot.
    private TimeSpan _averageSlotTime = MinimumWindow;

    // Raises and lowerings wait separately: a failed raise says nothing against lowering.
    private TimeSpan _raiseHold = InitialHold;
    private TimeSpan _raiseHoldUntil;
    private TimeSpan _lowerHold = InitialHold;
    private TimeSpan _lowerHoldUntil;

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
    private int _windowCpuStalledSamples;

    public AdaptiveParallelismPolicy(ParallelismLimits limits, int processorCount)
    {
        _limits = limits;
        _limit = limits.Initial;
        _cpuSaturationFloor = Math.Max(limits.Minimum, Math.Min(processorCount, limits.Initial));
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

        var hasMemoryHeadroom = _windowMaxMemoryLoad < MemoryHeadroomLoad;
        var hasCpuHeadroom = _windowCpuSeconds / _windowElapsed.TotalSeconds < CpuHeadroomUtilization
            && _windowMaxQueueDelay < QueueDelayLimit
            && _windowCpuStalledSamples * 4 < _windowSamples;

        ResetWindow();

        if (measurable)
        {
            _averageSlotTime = TimeSpan.FromSeconds(_limit / throughput);
        }

        if (_trial != AdaptiveLimitChange.None)
        {
            var raised = _limit > _limitBeforeTrial;
            _trial = AdaptiveLimitChange.None;

            // Negative for a lowering, so the same formula allows it a small loss.
            var change = (double) _limit / _limitBeforeTrial - 1;
            var requiredThroughput = raised
                ? _throughputBeforeTrial * (1 + change * MinimumGainShare)
                : _throughputBeforeLowering * (1 - (1 - CpuDecreaseFactor) * MinimumGainShare);

            if (!measurable || throughput < requiredThroughput)
            {
                if (raised)
                {
                    HoldRaises();
                    return ChangeLimit(_limitBeforeTrial, AdaptiveLimitChange.ProbeReverted);
                }

                _lowerHoldUntil = _clock + _lowerHold;
                _lowerHold = Double(_lowerHold);
                EndLoweringRunAt(_limitBeforeTrial);
                return ChangeLimit(_limitBeforeTrial, AdaptiveLimitChange.LowerReverted);
            }

            if (raised)
            {
                _raiseHold = InitialHold;
                EndLoweringRunAt(_limit);
            }
            else
            {
                // Fewer tests did as well, so trying more again straight away would only
                // undo it. The wait does not grow: nothing failed.
                _lowerHold = InitialHold;
                _raiseHoldUntil = _clock + _raiseHold;
            }
        }

        _measuredThroughput = throughput;
        _measuredLimit = measurable ? _limit : 0;

        if (!hasMemoryHeadroom)
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

        var higher = Math.Min(ceiling, Math.Max(_limit + 1, (int) Math.Ceiling(_limit * IncreaseFactor)));

        if (_limit < _limits.Initial)
        {
            higher = Math.Min(higher, _limits.Initial);

            if (hasCpuHeadroom)
            {
                EndLoweringRunAt(higher);
                return ChangeLimit(higher, AdaptiveLimitChange.Recovery);
            }

            // Busy CPUs are no reason to stay below the starting limit when the tests have
            // changed, or other processes are what keeps the CPUs busy. Without headroom the
            // way back is a trial like any other.
        }
        else if (!hasCpuHeadroom)
        {
            return NoChange();
        }

        if (!measurable || _clock < _raiseHoldUntil)
        {
            return NoChange();
        }

        return StartTrial(higher, AdaptiveLimitChange.Probe, throughput);
    }

    private AdaptiveDecision StartTrial(int limit, AdaptiveLimitChange change, double throughput)
    {
        // A raise on trial leaves the run of lowerings open: if it is put back, the limit is
        // still below where the run began.
        if (limit < _limit && _throughputBeforeLowering <= 0)
        {
            _throughputBeforeLowering = throughput;
            _limitBeforeLowering = _limit;
        }

        _trial = change;
        _limitBeforeTrial = _limit;
        _throughputBeforeTrial = throughput;
        return ChangeLimit(limit, change);
    }

    private AdaptiveDecision? TryRelievePressure(AdaptiveSample sample)
    {
        // Either the CPUs are all busy while work queues for a thread, or the operating
        // system says outright that tasks are waiting for a CPU.
        var saturated = sample.CpuStalled
            || (sample.CpuUtilization >= SaturatedCpuUtilization && sample.QueueDelay >= QueueDelayLimit);
        _saturatedSamples = saturated ? _saturatedSamples + 1 : 0;

        if (_limit <= _limits.Minimum || _clock < _pressureCooldownUntil)
        {
            return null;
        }

        if (sample.MemoryLoad >= MemoryPressureLoad)
        {
            return LowerForMemoryPressure();
        }

        // A lowering for CPU saturation is a trial, so it needs a throughput measurement at
        // this limit to be judged against, and no other trial in progress.
        if (_saturatedSamples >= SaturatedSamplesBeforeDecrease
            && _limit > _cpuSaturationFloor
            && _trial == AdaptiveLimitChange.None
            && !_settling
            && _measuredLimit == _limit
            && _clock >= _lowerHoldUntil)
        {
            _saturatedSamples = 0;
            return StartTrial(Math.Max(_cpuSaturationFloor, LowerBy(CpuDecreaseFactor)), AdaptiveLimitChange.CpuSaturation, _measuredThroughput);
        }

        return null;
    }

    private int LowerBy(double factor) => Math.Max(_limits.Minimum, Math.Min(_limit - 1, (int) (_limit * factor)));

    private AdaptiveDecision LowerForMemoryPressure()
    {
        var lowered = LowerBy(MemoryDecreaseFactor);

        // Halfway back is the highest the limit may return to while the pressure is recent, so
        // repeated pressure closes in on a limit the machine can hold.
        _pressureCap = Math.Max(lowered, (_limit + lowered) / 2);
        _pressureCapUntil = _clock + _raiseHold;
        _pressureCooldownUntil = _clock + PressureCooldown;
        _saturatedSamples = 0;
        _trial = AdaptiveLimitChange.None;
        _throughputBeforeLowering = 0;
        HoldRaises();

        return ChangeLimit(lowered, AdaptiveLimitChange.MemoryPressure);
    }

    // The run of lowerings is over once the limit is back at, or above, where it began.
    private void EndLoweringRunAt(int limit)
    {
        if (limit >= _limitBeforeLowering)
        {
            _throughputBeforeLowering = 0;
        }
    }

    private void HoldRaises()
    {
        _raiseHoldUntil = _clock + _raiseHold;
        _raiseHold = Double(_raiseHold);
    }

    private static TimeSpan Double(TimeSpan hold) => TimeSpan.FromTicks(Math.Min(hold.Ticks * 2, MaximumHold.Ticks));

    private AdaptiveDecision ChangeLimit(int limit, AdaptiveLimitChange change)
    {
        _limit = limit;
        _measuredLimit = 0;
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

        if (sample.CpuStalled)
        {
            _windowCpuStalledSamples++;
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
        _windowCpuStalledSamples = 0;
    }
}
