#if NET
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Measures how much of the whole machine's CPU capacity is in use, including other processes.
/// Test processes for several assemblies often run side by side, and tests start child
/// processes and containers, so this process's own CPU time understates the load.
/// </summary>
internal abstract class MachineCpuMonitor : IDisposable
{
    private ulong _previousBusy;
    private ulong _previousTotal;
    private bool _hasPrevious;

    /// <summary>
    /// Returns a monitor for this operating system, or <c>null</c> when it has none.
    /// </summary>
    public static MachineCpuMonitor? Create()
    {
        if (OperatingSystem.IsLinux())
        {
            return LinuxMonitor.TryCreate();
        }

        if (OperatingSystem.IsWindows())
        {
            // GetSystemTimes covers one processor group, which holds at most 64 logical
            // processors. On a larger machine it would describe only part of it.
            return Environment.ProcessorCount > WindowsMonitor.MaxProcessorsPerGroup ? null : new WindowsMonitor();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacMonitor();
        }

        return null;
    }

    /// <summary>
    /// The share of CPU capacity used since the previous call, 0 to 1. Returns <c>null</c> on
    /// the first call, when the operating system has not advanced its counters since the
    /// previous call, and when it gives no reading.
    /// </summary>
    public double? Sample()
    {
        if (!TryReadCounters(out var busy, out var total))
        {
            _hasPrevious = false;
            return null;
        }

        var hadPrevious = _hasPrevious;

        // Counters only grow, but macOS reports 32-bit ticks that wrap. Unsigned subtraction
        // gives the right difference across a wrap of either width.
        var busyDelta = CounterDelta(busy, _previousBusy);
        var totalDelta = CounterDelta(total, _previousTotal);

        _previousBusy = busy;
        _previousTotal = total;
        _hasPrevious = true;

        if (!hadPrevious || totalDelta == 0 || busyDelta > totalDelta)
        {
            return null;
        }

        return (double) busyDelta / totalDelta;
    }

    protected virtual ulong CounterDelta(ulong current, ulong previous) => current - previous;

    protected abstract bool TryReadCounters(out ulong busy, out ulong total);

    public virtual void Dispose()
    {
    }

    /// <summary>
    /// Parses the aggregate "cpu" line of /proc/stat: user nice system idle iowait irq softirq steal.
    /// </summary>
    internal static bool TryParseProcStat(ReadOnlySpan<byte> content, out ulong busy, out ulong total)
    {
        busy = 0;
        total = 0;

        if (!content.StartsWith("cpu "u8))
        {
            return false;
        }

        var lineEnd = content.IndexOf((byte) '\n');
        var line = lineEnd < 0 ? content[4..] : content[4..lineEnd];

        ulong idle = 0;
        var field = 0;
        var position = 0;

        while (position < line.Length)
        {
            if (line[position] == (byte) ' ')
            {
                position++;
                continue;
            }

            ulong value = 0;
            var start = position;
            while (position < line.Length && line[position] is >= (byte) '0' and <= (byte) '9')
            {
                value = value * 10 + (ulong) (line[position] - '0');
                position++;
            }

            if (position == start)
            {
                return false;
            }

            // Fields after steal (guest, guest_nice) are already counted in user and nice.
            if (field < 8)
            {
                total += value;
            }

            // idle and iowait: the CPU has nothing to run.
            if (field is 3 or 4)
            {
                idle += value;
            }

            field++;
        }

        // idle is the fourth field; older kernels stop there.
        if (field < 4)
        {
            return false;
        }

        busy = total - idle;
        return true;
    }

    [SupportedOSPlatform("linux")]
    private sealed class LinuxMonitor(SafeFileHandle handle) : MachineCpuMonitor
    {
        private readonly byte[] _buffer = new byte[512];

        public static LinuxMonitor? TryCreate()
        {
            try
            {
                return new LinuxMonitor(File.OpenHandle("/proc/stat"));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        protected override bool TryReadCounters(out ulong busy, out ulong total)
        {
            // procfs generates the content again on every read from offset 0.
            var read = RandomAccess.Read(handle, _buffer, fileOffset: 0);
            return TryParseProcStat(_buffer.AsSpan(0, read), out busy, out total);
        }

        public override void Dispose() => handle.Dispose();
    }

    [SupportedOSPlatform("windows")]
    private sealed class WindowsMonitor : MachineCpuMonitor
    {
        public const int MaxProcessorsPerGroup = 64;

        protected override bool TryReadCounters(out ulong busy, out ulong total)
        {
            // Kernel time includes idle time.
            if (!GetSystemTimes(out var idle, out var kernel, out var user))
            {
                busy = 0;
                total = 0;
                return false;
            }

            total = kernel + user;
            busy = total - idle;
            return true;
        }

        // Each FILETIME is a 64-bit count of 100 ns intervals.
        [DllImport("kernel32.dll", SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime);
    }

    [SupportedOSPlatform("macos")]
    private sealed class MacMonitor : MachineCpuMonitor
    {
        private const int HostCpuLoadInfo = 3;
        private const int CpuStateUser = 0;
        private const int CpuStateSystem = 1;
        private const int CpuStateIdle = 2;
        private const int CpuStateNice = 3;
        private const int CpuStateCount = 4;

        // mach_host_self adds a reference to the host port on every call. Taking it once per
        // process means monitors hold nothing of their own to release.
        private static readonly uint Host = mach_host_self();

        private readonly uint[] _ticks = new uint[CpuStateCount];

        protected override bool TryReadCounters(out ulong busy, out ulong total)
        {
            var count = (uint) CpuStateCount;
            if (host_statistics(Host, HostCpuLoadInfo, _ticks, ref count) != 0 || count < CpuStateCount)
            {
                busy = 0;
                total = 0;
                return false;
            }

            busy = (ulong) _ticks[CpuStateUser] + _ticks[CpuStateSystem] + _ticks[CpuStateNice];
            total = busy + _ticks[CpuStateIdle];
            return true;
        }

        // The sums of 32-bit tick counters wrap at 32 bits too, as long as fewer than 2^32
        // ticks pass between two samples.
        protected override ulong CounterDelta(ulong current, ulong previous) => (uint) (current - previous);

        [DllImport("libSystem.dylib")]
        private static extern uint mach_host_self();

        [DllImport("libSystem.dylib")]
        private static extern int host_statistics(uint host, int flavor, [Out] uint[] info, ref uint count);
    }
}
#endif
