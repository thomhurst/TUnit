#if NET
using System.Globalization;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Reads CPU, memory and stall counters from procfs and, in a cgroup v2 hierarchy, from the
/// control groups this process runs in. A container's memory limit replaces the host's memory,
/// and its CPU quota shows up as throttled time.
/// </summary>
internal sealed class LinuxMachineResourceReader : IMachineResourceReader
{
    private readonly Func<string, string?> _readFile;

    // The process's control group and the mount it is visible under. Found once: a test
    // process is not moved between control groups while it runs.
    private readonly string? _cgroupDirectory;
    private readonly string? _cgroupMount;

    public LinuxMachineResourceReader() : this(File.ReadAllText, Environment.ProcessId)
    {
    }

    internal LinuxMachineResourceReader(Func<string, string?> readFile, int processId)
    {
        _readFile = readFile;
        (_cgroupDirectory, _cgroupMount) = FindCgroup(processId);
    }

    public bool TryRead(out MachineResourceReading reading)
    {
        reading = default;

        if (!TryParseCpu(FirstLine(Read("/proc/stat")), out var idle, out var total)
            || !TryParseMemory(Lines(Read("/proc/meminfo")), out var memoryTotal, out var memoryAvailable))
        {
            return false;
        }

        (memoryTotal, memoryAvailable) = ReadCgroupMemory(memoryTotal, memoryAvailable);

        // CPU pressure of the process's own control group says whether its tasks wait for a
        // CPU; the host's figure also counts other tenants' tasks, so it is the fallback only.
        var cpuStall = FindPressureTotal(ReadCgroupFile("cpu.pressure"));
        var hostCpuStall = cpuStall is null ? FindPressureTotal(Read("/proc/pressure/cpu")) : null;

        // Memory is different: a host that runs out takes every container with it, so both
        // figures count.
        reading = new MachineResourceReading(
            idle,
            total,
            memoryTotal,
            memoryAvailable,
            ReadThrottledMicroseconds(out var throttledSources),
            cpuStall,
            FindPressureTotal(ReadCgroupFile("memory.pressure")),
            hostCpuStall,
            FindPressureTotal(Read("/proc/pressure/memory")),
            throttledSources);
        return true;
    }

    // cpu.stat counts only the throttling of the quota set on that control group, so a quota
    // on a parent shows in the parent's file. The sum covers every level that is visible.
    // The sources value has one bit per level that contributed, the process's own group first,
    // so the sampler can tell when the sum is made of different groups than last time.
    private long? ReadThrottledMicroseconds(out int sources)
    {
        long? throttled = null;
        sources = 0;
        var level = 0;

        for (var directory = _cgroupDirectory; directory is not null && _cgroupMount is not null; directory = GetParent(directory), level++)
        {
            if (FindCounter(Read(Combine(directory, "cpu.stat")), "throttled_usec") is { } value)
            {
                throttled = (throttled ?? 0) + value;

                // Hierarchies deeper than 31 levels share the last bit.
                sources |= 1 << Math.Min(level, 30);
            }

            if (directory == _cgroupMount)
            {
                break;
            }
        }

        return throttled;
    }

    // The host and each visible control group are compared against their own limits, and the
    // one closest to its limit supplies the reading. Siblings can exhaust a parent's limit
    // before this group reaches its own, and a container with room left does not help when
    // the host itself is nearly out of memory.
    private (long Total, long Available) ReadCgroupMemory(long hostTotal, long hostAvailable)
    {
        long total = hostTotal, available = hostAvailable;

        for (var directory = _cgroupDirectory; directory is not null && _cgroupMount is not null; directory = GetParent(directory))
        {
            if (TryNonNegative(Read(Combine(directory, "memory.max"))?.Trim(), out var limit)
                && limit > 0
                && limit <= hostTotal
                && TryNonNegative(Read(Combine(directory, "memory.current"))?.Trim(), out var used))
            {
                // File cache the kernel can drop at once is not memory in use.
                var inactiveFile = FindCounter(Read(Combine(directory, "memory.stat")), "inactive_file") ?? 0;
                used -= Math.Min(used, inactiveFile);

                var headroom = Math.Clamp(limit - used, 0, limit);
                if ((double) headroom / limit < (double) available / total)
                {
                    total = limit;
                    available = headroom;
                }
            }

            if (directory == _cgroupMount)
            {
                break;
            }
        }

        return (total, available);
    }

    private string? ReadCgroupFile(string name) => _cgroupDirectory is null ? null : Read(Combine(_cgroupDirectory, name));

    private (string? Directory, string? Mount) FindCgroup(int processId)
    {
        // cgroup v2 has a single line, "0::/path". cgroup v1 limits are not read.
        string? membership = null;
        foreach (var line in Lines(Read("/proc/self/cgroup")))
        {
            if (line.StartsWith("0::", StringComparison.Ordinal))
            {
                membership = line[3..];
                break;
            }
        }

        if (membership is null || !membership.StartsWith('/') || membership.Split('/').Contains(".."))
        {
            return default;
        }

        foreach (var line in Lines(Read("/proc/self/mountinfo")))
        {
            var parts = line.Split(" - ", 2, StringSplitOptions.None);
            if (parts.Length != 2 || !parts[1].StartsWith("cgroup2 ", StringComparison.Ordinal))
            {
                continue;
            }

            var fields = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 5)
            {
                continue;
            }

            var root = UnescapeMount(fields[3]).TrimEnd('/');
            var mount = UnescapeMount(fields[4]);
            mount = mount.Length > 1 ? mount.TrimEnd('/') : mount;

            string relative;
            if (root.Length == 0)
            {
                relative = membership;
            }
            else if (membership == root || membership.StartsWith(root + "/", StringComparison.Ordinal))
            {
                relative = membership[root.Length..];
            }
            else if (membership == "/" && IsMember(mount, processId))
            {
                // Inside a cgroup namespace the membership is "/" whatever subtree is mounted,
                // so accept the mount only when it lists this process.
                relative = string.Empty;
            }
            else
            {
                continue;
            }

            if (mount.StartsWith('/'))
            {
                return (Combine(mount, relative.TrimStart('/')), mount);
            }
        }

        return default;
    }

    private bool IsMember(string mount, int processId)
    {
        foreach (var line in Lines(Read(Combine(mount, "cgroup.procs"))))
        {
            if (TryNonNegative(line.Trim(), out var value) && value == processId)
            {
                return true;
            }
        }

        return false;
    }

    private string? Read(string path)
    {
        try
        {
            return _readFile(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string Combine(string directory, string relative)
        => relative.Length == 0 ? directory : $"{directory.TrimEnd('/')}/{relative}";

    private static string? GetParent(string directory)
    {
        var separator = directory.LastIndexOf('/');
        return directory == "/" || separator < 0 ? null : separator == 0 ? "/" : directory[..separator];
    }

    private static string UnescapeMount(string value) => value
        .Replace("\\040", " ", StringComparison.Ordinal)
        .Replace("\\011", "\t", StringComparison.Ordinal)
        .Replace("\\012", "\n", StringComparison.Ordinal)
        .Replace("\\134", "\\", StringComparison.Ordinal);

    private static string[] Lines(string? content) => content?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? [];

    private static string? FirstLine(string? content)
    {
        if (content is null)
        {
            return null;
        }

        var lineEnd = content.IndexOf('\n');
        return lineEnd < 0 ? content : content[..lineEnd];
    }

    private static bool TryNonNegative(string? value, out long result)
        => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result) && result >= 0;

    private static long? FindCounter(string? content, string name)
    {
        foreach (var line in Lines(content))
        {
            var fields = line.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 2 && fields[0] == name && TryNonNegative(fields[1], out var value))
            {
                return value;
            }
        }

        return null;
    }

    // "some avg10=0.00 avg60=0.00 avg300=0.00 total=12345": total is the microseconds for which
    // at least one task was stalled.
    private static long? FindPressureTotal(string? content)
    {
        foreach (var line in Lines(content))
        {
            if (!line.StartsWith("some ", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var field in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (field.StartsWith("total=", StringComparison.Ordinal))
                {
                    return TryNonNegative(field[6..], out var value) ? value : null;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Parses the aggregate "cpu" line of /proc/stat: user, nice, system, idle, iowait, irq,
    /// softirq, steal, then guest fields that user and nice already include.
    /// </summary>
    internal static bool TryParseCpu(string? line, out long idle, out long total)
    {
        idle = 0;
        total = 0;

        var fields = line?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields is not { Length: >= 5 } || fields[0] != "cpu")
        {
            return false;
        }

        for (var index = 1; index < fields.Length && index <= 8; index++)
        {
            if (!long.TryParse(fields[index], NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                return false;
            }

            total += value;

            // Fields 4 and 5 are idle and iowait: the CPU has nothing to run.
            if (index is 4 or 5)
            {
                idle += value;
            }
        }

        return true;
    }

    internal static bool TryParseMemory(IReadOnlyList<string> lines, out long totalBytes, out long availableBytes)
    {
        totalBytes = FindKilobytes(lines, "MemTotal:") * 1024;
        availableBytes = FindKilobytes(lines, "MemAvailable:") * 1024;
        return totalBytes > 0 && availableBytes >= 0;
    }

    private static long FindKilobytes(IReadOnlyList<string> lines, string name)
    {
        foreach (var line in lines)
        {
            if (!line.StartsWith(name, StringComparison.Ordinal))
            {
                continue;
            }

            var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return fields.Length >= 2 && long.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out var kilobytes)
                ? kilobytes
                : -1;
        }

        return -1;
    }
}
#endif
