#if NET
using TUnit.Engine.Scheduling;

namespace TUnit.UnitTests;

public class LinuxMachineResourceReaderTests
{
    private const int ProcessId = 4242;
    private const long Gigabyte = 1024L * 1024 * 1024;

    // 16 GB in total, 4 GB available.
    private const string ProcStat = "cpu  100 20 30 800 40 5 3 2 50 0\ncpu0 50 10 15 400 20 2 1 1 25 0\n";
    private const string MemInfo = "MemTotal:       16777216 kB\nMemFree:         1048576 kB\nMemAvailable:    4194304 kB\n";
    private const string CgroupV2Mount = "30 23 0:26 / /sys/fs/cgroup rw,nosuid,nodev,noexec,relatime shared:4 - cgroup2 cgroup2 rw\n";

    [Test]
    public async Task Host_Without_Control_Groups_Reports_Cpu_And_Memory()
    {
        var reader = Reader(new()
        {
            ["/proc/stat"] = ProcStat,
            ["/proc/meminfo"] = MemInfo,
        });

        var read = reader.TryRead(out var reading);

        await Assert.That(read).IsTrue();
        // user nice system idle iowait irq softirq steal; guest fields are already in user and nice.
        await Assert.That(reading.CpuTotal).IsEqualTo(1000);
        await Assert.That(reading.CpuIdle).IsEqualTo(840);
        await Assert.That(reading.MemoryTotalBytes).IsEqualTo(16 * Gigabyte);
        await Assert.That(reading.MemoryAvailableBytes).IsEqualTo(4 * Gigabyte);
        await Assert.That(reading.ThrottledMicroseconds).IsNull();
        await Assert.That(reading.CpuStallMicroseconds).IsNull();
        await Assert.That(reading.MemoryStallMicroseconds).IsNull();
        await Assert.That(reading.HostCpuStallMicroseconds).IsNull();
        await Assert.That(reading.HostMemoryStallMicroseconds).IsNull();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("cpu0 1 2 3 4 5\n")]
    [Arguments("cpu  1 2 3\n")]
    [Arguments("cpu  1 x 3 4 5\n")]
    public async Task Unreadable_Cpu_Counters_Give_No_Reading(string? procStat)
    {
        var reader = Reader(new()
        {
            ["/proc/stat"] = procStat,
            ["/proc/meminfo"] = MemInfo,
        });

        await Assert.That(reader.TryRead(out _)).IsFalse();
    }

    [Test]
    public async Task A_Container_With_Room_Left_Does_Not_Hide_An_Exhausted_Host()
    {
        // The container uses half of its limit, but the host has under 3% left.
        var reader = Reader(new()
        {
            ["/proc/stat"] = ProcStat,
            ["/proc/meminfo"] = "MemTotal:       16777216 kB\nMemAvailable:      419430 kB\n",
            ["/proc/self/cgroup"] = "0::/\n",
            ["/proc/self/mountinfo"] = CgroupV2Mount,
            ["/sys/fs/cgroup/cgroup.procs"] = $"{ProcessId}\n",
            ["/sys/fs/cgroup/memory.max"] = $"{2 * Gigabyte}\n",
            ["/sys/fs/cgroup/memory.current"] = $"{Gigabyte}\n",
        });

        reader.TryRead(out var reading);

        await Assert.That(reading.MemoryTotalBytes).IsEqualTo(16 * Gigabyte);
        await Assert.That(reading.MemoryAvailableBytes).IsEqualTo(419430L * 1024);
    }

    [Test]
    public async Task A_Container_Closer_To_Its_Limit_Than_The_Host_Supplies_Memory()
    {
        var reader = Reader(new()
        {
            ["/proc/stat"] = ProcStat,
            ["/proc/meminfo"] = MemInfo,
            ["/proc/self/cgroup"] = "0::/\n",
            ["/proc/self/mountinfo"] = CgroupV2Mount,
            ["/sys/fs/cgroup/cgroup.procs"] = $"1\n{ProcessId}\n",
            ["/sys/fs/cgroup/memory.max"] = $"{2 * Gigabyte}\n",
            ["/sys/fs/cgroup/memory.current"] = $"{2 * Gigabyte}\n",
            // File cache the kernel can drop at once is not memory in use.
            ["/sys/fs/cgroup/memory.stat"] = $"anon 1000\ninactive_file {Gigabyte / 4}\nactive_file 10\n",
        });

        reader.TryRead(out var reading);

        // An eighth of the container's limit is left, against a quarter of the host's memory.
        await Assert.That(reading.MemoryTotalBytes).IsEqualTo(2 * Gigabyte);
        await Assert.That(reading.MemoryAvailableBytes).IsEqualTo(Gigabyte / 4);
    }

    [Test]
    public async Task Unlimited_Container_Memory_Falls_Back_To_The_Host()
    {
        var reader = Reader(new()
        {
            ["/proc/stat"] = ProcStat,
            ["/proc/meminfo"] = MemInfo,
            ["/proc/self/cgroup"] = "0::/\n",
            ["/proc/self/mountinfo"] = CgroupV2Mount,
            ["/sys/fs/cgroup/cgroup.procs"] = $"{ProcessId}\n",
            ["/sys/fs/cgroup/memory.max"] = "max\n",
            ["/sys/fs/cgroup/memory.current"] = $"{Gigabyte}\n",
        });

        reader.TryRead(out var reading);

        await Assert.That(reading.MemoryTotalBytes).IsEqualTo(16 * Gigabyte);
        await Assert.That(reading.MemoryAvailableBytes).IsEqualTo(4 * Gigabyte);
    }

    [Test]
    public async Task The_Control_Group_Closest_To_Its_Limit_Supplies_Memory()
    {
        // The process's own group has plenty left, but its siblings have nearly used up the parent.
        var reader = Reader(new()
        {
            ["/proc/stat"] = ProcStat,
            ["/proc/meminfo"] = MemInfo,
            ["/proc/self/cgroup"] = "0::/parent/child\n",
            ["/proc/self/mountinfo"] = CgroupV2Mount,
            ["/sys/fs/cgroup/parent/child/memory.max"] = $"{4 * Gigabyte}\n",
            ["/sys/fs/cgroup/parent/child/memory.current"] = $"{Gigabyte}\n",
            ["/sys/fs/cgroup/parent/memory.max"] = $"{8 * Gigabyte}\n",
            ["/sys/fs/cgroup/parent/memory.current"] = $"{7 * Gigabyte}\n",
        });

        reader.TryRead(out var reading);

        await Assert.That(reading.MemoryTotalBytes).IsEqualTo(8 * Gigabyte);
        await Assert.That(reading.MemoryAvailableBytes).IsEqualTo(Gigabyte);
    }

    [Test]
    public async Task Throttled_Time_Is_Summed_Over_The_Group_And_Its_Parents()
    {
        // A quota throttles at the group it is set on, so a parent's quota shows in the parent's file.
        var reader = Reader(new()
        {
            ["/proc/stat"] = ProcStat,
            ["/proc/meminfo"] = MemInfo,
            ["/proc/self/cgroup"] = "0::/parent/child\n",
            ["/proc/self/mountinfo"] = CgroupV2Mount,
            ["/sys/fs/cgroup/parent/child/cpu.stat"] = "usage_usec 900\nnr_periods 12\nnr_throttled 3\nthrottled_usec 250000\n",
            ["/sys/fs/cgroup/parent/cpu.stat"] = "usage_usec 900\nnr_periods 12\nnr_throttled 1\nthrottled_usec 50000\n",
            // No cpu controller at the root: usage only.
            ["/sys/fs/cgroup/cpu.stat"] = "usage_usec 900\nuser_usec 700\nsystem_usec 200\n",
        });

        reader.TryRead(out var reading);

        await Assert.That(reading.ThrottledMicroseconds).IsEqualTo(300_000);
    }

    [Test]
    public async Task A_Parent_That_Becomes_Readable_Again_Is_Not_New_Throttling()
    {
        var files = new Dictionary<string, string?>
        {
            ["/proc/stat"] = ProcStat,
            ["/proc/meminfo"] = MemInfo,
            ["/proc/self/cgroup"] = "0::/parent/child\n",
            ["/proc/self/mountinfo"] = CgroupV2Mount,
            ["/sys/fs/cgroup/parent/child/cpu.stat"] = "throttled_usec 1000\n",
        };
        var sampler = new MachineResourceSampler(Reader(files));
        var second = System.Diagnostics.Stopwatch.Frequency;

        // The parent's file cannot be read at first, so only the child is in the sum.
        sampler.Sample(1);

        // It can now, and brings its whole history with it. Neither counter has advanced.
        files["/sys/fs/cgroup/parent/cpu.stat"] = "throttled_usec 90000000\n";
        await Assert.That(sampler.Sample(1 + second).CpuStalled).IsFalse();

        // With the same groups in both sums, new throttling counts again.
        files["/sys/fs/cgroup/parent/cpu.stat"] = "throttled_usec 90500000\n";
        await Assert.That(sampler.Sample(1 + 2 * second).CpuStalled).IsTrue();
    }

    [Test]
    public async Task Pressure_Comes_From_The_Control_Group_When_It_Has_Any()
    {
        var reader = Reader(new()
        {
            ["/proc/stat"] = ProcStat,
            ["/proc/meminfo"] = MemInfo,
            ["/proc/self/cgroup"] = "0::/job\n",
            ["/proc/self/mountinfo"] = CgroupV2Mount,
            ["/sys/fs/cgroup/job/cpu.pressure"] = "some avg10=1.50 avg60=0.40 avg300=0.10 total=7000\nfull avg10=0.00 avg60=0.00 avg300=0.00 total=10\n",
            ["/proc/pressure/cpu"] = "some avg10=9.00 avg60=9.00 avg300=9.00 total=999999\n",
            ["/proc/pressure/memory"] = "some avg10=0.00 avg60=0.00 avg300=0.00 total=42\nfull avg10=0.00 avg60=0.00 avg300=0.00 total=7\n",
        });

        reader.TryRead(out var reading);

        // The group's own CPU pressure is used, so the host's is not read.
        await Assert.That(reading.CpuStallMicroseconds).IsEqualTo(7000);
        await Assert.That(reading.HostCpuStallMicroseconds).IsNull();
        // The group has no memory.pressure file. The host's is reported as the host's.
        await Assert.That(reading.MemoryStallMicroseconds).IsNull();
        await Assert.That(reading.HostMemoryStallMicroseconds).IsEqualTo(42);
    }

    [Test]
    public async Task A_Mount_That_Does_Not_List_This_Process_Is_Not_Its_Control_Group()
    {
        // Inside a cgroup namespace the membership is "/" for any mounted subtree.
        var reader = Reader(new()
        {
            ["/proc/stat"] = ProcStat,
            ["/proc/meminfo"] = MemInfo,
            ["/proc/self/cgroup"] = "0::/\n",
            ["/proc/self/mountinfo"] = "30 23 0:26 /other /sys/fs/cgroup rw - cgroup2 cgroup2 rw\n",
            ["/sys/fs/cgroup/cgroup.procs"] = "1\n2\n",
            ["/sys/fs/cgroup/memory.max"] = $"{2 * Gigabyte}\n",
            ["/sys/fs/cgroup/memory.current"] = $"{Gigabyte}\n",
            ["/sys/fs/cgroup/cpu.stat"] = "throttled_usec 250000\n",
        });

        reader.TryRead(out var reading);

        await Assert.That(reading.MemoryTotalBytes).IsEqualTo(16 * Gigabyte);
        await Assert.That(reading.ThrottledMicroseconds).IsNull();
    }

    [Test]
    public async Task Control_Groups_V1_Are_Ignored()
    {
        var reader = Reader(new()
        {
            ["/proc/stat"] = ProcStat,
            ["/proc/meminfo"] = MemInfo,
            ["/proc/self/cgroup"] = "4:cpu,cpuacct:/docker/abc\n3:memory:/docker/abc\n",
            ["/proc/self/mountinfo"] = "31 23 0:27 / /sys/fs/cgroup/cpu,cpuacct rw - cgroup cgroup rw,cpu,cpuacct\n",
        });

        var read = reader.TryRead(out var reading);

        await Assert.That(read).IsTrue();
        await Assert.That(reading.MemoryTotalBytes).IsEqualTo(16 * Gigabyte);
        await Assert.That(reading.ThrottledMicroseconds).IsNull();
    }

    private static LinuxMachineResourceReader Reader(Dictionary<string, string?> files)
        => new(path => files.TryGetValue(path, out var content) ? content : throw new FileNotFoundException(path), ProcessId);
}
#endif
