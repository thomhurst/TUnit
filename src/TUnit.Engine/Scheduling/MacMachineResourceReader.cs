#if NET
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TUnit.Engine.Scheduling;

[SupportedOSPlatform("macos")]
internal sealed class MacMachineResourceReader : IMachineResourceReader
{
    private const string SystemLibrary = "/usr/lib/libSystem.dylib";
    private const int HostCpuLoadInfo = 3;

    // host_cpu_load_info: user, system, idle and nice ticks.
    private const int CpuLoadInfoCount = 4;
    private const int CpuIdleIndex = 2;

    // kern.memorystatus_vm_pressure_level: 1 is normal, 2 a warning, 4 critical.
    private const int PressureWarning = 2;
    private const int PressureCritical = 4;

    // What a warning leaves available at most: enough to stop the limit being raised, not
    // enough to count as memory pressure.
    private const double WarningAvailableShare = 0.1;

    // mach_host_self adds a reference to the host port on every call. Taking it once per
    // process means readers hold nothing of their own to release.
    private static readonly uint Host = mach_host_self();

    private readonly long _totalMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
    private readonly uint[] _cpu = new uint[CpuLoadInfoCount];

    public bool TryRead(out MachineResourceReading reading)
    {
        reading = default;

        if (_totalMemoryBytes <= 0)
        {
            return false;
        }

        var cpuCount = (uint) CpuLoadInfoCount;

        if (host_statistics(Host, HostCpuLoadInfo, _cpu, ref cpuCount) != 0
            || cpuCount < CpuLoadInfoCount
            || !TryReadInt32("kern.memorystatus_level", out var freePercentage))
        {
            return false;
        }

        long idle = _cpu[CpuIdleIndex];
        long total = 0;
        foreach (var ticks in _cpu)
        {
            total += ticks;
        }

        // The kernel's own figure for the share of memory it can still hand out. Counting
        // pages instead (app, wired and compressed memory, as Activity Monitor does) reads
        // far higher on a healthy machine, because macOS keeps memory in use on purpose.
        var available = (long) (_totalMemoryBytes * (Math.Clamp(freePercentage, 0, 100) / 100.0));

        // The kernel's verdict overrides the figure: it accounts for how fast memory can be
        // reclaimed, which a free percentage does not.
        if (TryReadInt32("kern.memorystatus_vm_pressure_level", out var pressureLevel))
        {
            if (pressureLevel >= PressureCritical)
            {
                available = 0;
            }
            else if (pressureLevel >= PressureWarning)
            {
                available = Math.Min(available, (long) (_totalMemoryBytes * WarningAvailableShare));
            }
        }

        reading = new MachineResourceReading(idle, total, _totalMemoryBytes, available);
        return true;
    }

    private static bool TryReadInt32(string name, out int value)
    {
        var length = (nuint) sizeof(int);
        return sysctlbyname(name, out value, ref length, IntPtr.Zero, 0) == 0 && length == sizeof(int);
    }

    [DllImport(SystemLibrary)]
    private static extern uint mach_host_self();

    [DllImport(SystemLibrary)]
    private static extern int host_statistics(uint host, int flavor, [Out] uint[] info, ref uint count);

    [DllImport(SystemLibrary, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern int sysctlbyname(string name, out int oldValue, ref nuint oldLength, IntPtr newValue, nuint newLength);
}
#endif
