#if NET
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TUnit.Engine.Scheduling;

[SupportedOSPlatform("windows")]
internal sealed class WindowsMachineResourceReader : IMachineResourceReader
{
    // SystemProcessorPerformanceInformation.
    private const int ProcessorPerformanceInformation = 8;

    // A processor group holds at most 64 logical processors.
    private const int MaxProcessorsPerGroup = 64;

    private readonly ProcessorPerformance[] _processors = new ProcessorPerformance[MaxProcessorsPerGroup];

    public bool TryRead(out MachineResourceReading reading)
    {
        reading = default;

        var memory = new MemoryStatusEx { Length = (uint) Marshal.SizeOf<MemoryStatusEx>() };

        if (!TryReadProcessorTimes(out var idle, out var total) || !GlobalMemoryStatusEx(ref memory))
        {
            return false;
        }

        reading = new MachineResourceReading(idle, total, (long) memory.TotalPhysical, (long) memory.AvailablePhysical);
        return true;
    }

    // GetSystemTimes covers only the calling thread's processor group, so on a machine with
    // more than 64 logical processors it describes part of it. This reads every group.
    private bool TryReadProcessorTimes(out long idle, out long total)
    {
        idle = 0;
        total = 0;

        var groupCount = GetActiveProcessorGroupCount();
        if (groupCount == 0)
        {
            return false;
        }

        var entrySize = (uint) Marshal.SizeOf<ProcessorPerformance>();
        long idleSum = 0;
        long totalSum = 0;

        for (ushort group = 0; group < groupCount; group++)
        {
            var status = NtQuerySystemInformationEx(
                ProcessorPerformanceInformation,
                ref group,
                sizeof(ushort),
                _processors,
                (uint) _processors.Length * entrySize,
                out var bytesWritten);

            if (status != 0 || bytesWritten == 0 || bytesWritten > _processors.Length * entrySize || bytesWritten % entrySize != 0)
            {
                // A reading that leaves a group out would describe part of the machine.
                return false;
            }

            for (var index = 0; index < bytesWritten / entrySize; index++)
            {
                idleSum += _processors[index].IdleTime;

                // Kernel time includes idle time.
                totalSum += _processors[index].KernelTime + _processors[index].UserTime;
            }
        }

        idle = idleSum;
        total = totalSum;
        return true;
    }

    [DllImport("kernel32.dll")]
    private static extern ushort GetActiveProcessorGroupCount();

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformationEx(
        int informationClass,
        ref ushort group,
        uint inputLength,
        [Out] ProcessorPerformance[] buffer,
        uint bufferLength,
        out uint returnLength);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    // SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION, with its reserved fields and tail padding.
    [StructLayout(LayoutKind.Sequential, Size = 48)]
    private struct ProcessorPerformance
    {
        public long IdleTime;
        public long KernelTime;
        public long UserTime;
        private long _reserved1;
        private long _reserved2;
        private uint _reserved3;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
}
#endif
