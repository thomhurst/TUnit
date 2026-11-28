#if NET
using System.Diagnostics;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Cumulative machine-wide counters at one point in time. The two CPU values share an
/// arbitrary unit. The stall counters exist on Linux only.
/// </summary>
/// <param name="CpuIdle">CPU time spent idle.</param>
/// <param name="CpuTotal">All CPU time, idle included.</param>
/// <param name="MemoryTotalBytes">Memory the process may use: the machine's, or a container limit.</param>
/// <param name="MemoryAvailableBytes">Memory still available within that total.</param>
/// <param name="ThrottledMicroseconds">How long a CPU quota has throttled this process's control groups.</param>
/// <param name="CpuStallMicroseconds">How long at least one task in this process's control group has waited for a CPU.</param>
/// <param name="MemoryStallMicroseconds">How long at least one task in this process's control group has waited for memory to be reclaimed.</param>
/// <param name="HostCpuStallMicroseconds">The same for the whole host. Read only when the control group's figure is not available.</param>
/// <param name="HostMemoryStallMicroseconds">The same for the whole host.</param>
/// <param name="ThrottledSources">Identifies which control groups <paramref name="ThrottledMicroseconds"/> adds up. Two totals compare only when it is the same.</param>
internal readonly record struct MachineResourceReading(
    long CpuIdle,
    long CpuTotal,
    long MemoryTotalBytes,
    long MemoryAvailableBytes,
    long? ThrottledMicroseconds = null,
    long? CpuStallMicroseconds = null,
    long? MemoryStallMicroseconds = null,
    long? HostCpuStallMicroseconds = null,
    long? HostMemoryStallMicroseconds = null,
    int ThrottledSources = 0);

internal interface IMachineResourceReader
{
    bool TryRead(out MachineResourceReading reading);
}

/// <summary>
/// What the machine was doing between two samples.
/// </summary>
/// <param name="CpuUtilization">Share of CPU capacity in use, 0 to 1, or <c>null</c> without a reading.</param>
/// <param name="MemoryUsed">Share of memory in use, 0 to 1, or <c>null</c> without a reading.</param>
/// <param name="CpuStalled">Tasks waited for a CPU, or a CPU quota throttled the process, for a notable share of the time.</param>
/// <param name="MemoryStalled">Tasks waited for memory to be reclaimed for a notable share of the time.</param>
internal readonly record struct MachineLoad(double? CpuUtilization, double? MemoryUsed, bool CpuStalled, bool MemoryStalled);

/// <summary>
/// Turns the cumulative counters of an <see cref="IMachineResourceReader"/> into the load since
/// the previous sample. The readings cover the whole machine, because test processes for several
/// assemblies often run side by side, and tests start child processes and containers that this
/// process's own CPU time and managed heap do not show.
/// </summary>
// The readers follow ModularPipelines' machine resource monitoring
// (https://github.com/thomhurst/ModularPipelines, src/ModularPipelines/Engine/Resources).
internal sealed class MachineResourceSampler
{
    // A stall or throttle shorter than this share of the time between samples is routine:
    // brief contention and occasional quota hits happen on a healthy machine.
    internal const double StallShare = 0.1;

    private readonly IMachineResourceReader _reader;
    private MachineResourceReading _previous;
    private long _previousTimestamp;
    private bool _hasPrevious;

    public MachineResourceSampler(IMachineResourceReader reader)
    {
        _reader = reader;
    }

    /// <summary>
    /// Returns a sampler for this operating system, or <c>null</c> when it has no reader.
    /// </summary>
    public static MachineResourceSampler? Create()
    {
        if (OperatingSystem.IsLinux())
        {
            return new MachineResourceSampler(new LinuxMachineResourceReader());
        }

        if (OperatingSystem.IsWindows())
        {
            return new MachineResourceSampler(new WindowsMachineResourceReader());
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MachineResourceSampler(new MacMachineResourceReader());
        }

        return null;
    }

    public MachineLoad Sample() => Sample(Stopwatch.GetTimestamp());

    internal MachineLoad Sample(long timestamp)
    {
        if (!_reader.TryRead(out var reading))
        {
            _hasPrevious = false;
            return default;
        }

        var previous = _previous;
        var hadPrevious = _hasPrevious;
        var elapsedMicroseconds = Stopwatch.GetElapsedTime(_previousTimestamp, timestamp).TotalMicroseconds;

        _previous = reading;
        _previousTimestamp = timestamp;
        _hasPrevious = true;

        double? memoryUsed = reading.MemoryTotalBytes > 0
            ? 1 - Math.Clamp((double) reading.MemoryAvailableBytes / reading.MemoryTotalBytes, 0, 1)
            : null;

        if (!hadPrevious)
        {
            // Counters since boot say nothing about now.
            return new MachineLoad(null, memoryUsed, false, false);
        }

        var total = reading.CpuTotal - previous.CpuTotal;
        var idle = reading.CpuIdle - previous.CpuIdle;

        // The operating system does not advance its counters on every sample (macOS can go a
        // second without), and macOS counters wrap. Neither gives a reading for this sample.
        double? cpuUtilization = total > 0 && idle >= 0 && idle <= total
            ? 1 - (double) idle / total
            : null;

        // Each counter is compared only with its own previous value, so a reader that switches
        // from one source to another never subtracts two unrelated totals.
        // The throttled total adds up several control groups. When one of them drops out of the
        // sum or comes back, the totals differ by its whole history, not by new throttling.
        var cpuStalled = (reading.ThrottledSources == previous.ThrottledSources
                && Stalled(reading.ThrottledMicroseconds, previous.ThrottledMicroseconds, elapsedMicroseconds))
            || Stalled(reading.CpuStallMicroseconds, previous.CpuStallMicroseconds, elapsedMicroseconds)
            || Stalled(reading.HostCpuStallMicroseconds, previous.HostCpuStallMicroseconds, elapsedMicroseconds);
        var memoryStalled = Stalled(reading.MemoryStallMicroseconds, previous.MemoryStallMicroseconds, elapsedMicroseconds)
            || Stalled(reading.HostMemoryStallMicroseconds, previous.HostMemoryStallMicroseconds, elapsedMicroseconds);

        return new MachineLoad(cpuUtilization, memoryUsed, cpuStalled, memoryStalled);
    }

    // A counter that appears, disappears or goes backwards (its source changed) has no
    // meaningful difference.
    private static bool Stalled(long? current, long? previous, double elapsedMicroseconds)
        => current is { } now
           && previous is { } before
           && now > before
           && elapsedMicroseconds > 0
           && now - before >= elapsedMicroseconds * StallShare;
}
#endif
