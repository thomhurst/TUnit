#if NET
using System.Diagnostics;
using TUnit.Engine.Scheduling;

namespace TUnit.UnitTests;

public class MachineResourceSamplerTests
{
    private const long Gigabyte = 1024L * 1024 * 1024;

    [Test]
    public async Task First_Sample_Reports_Memory_But_No_Cpu()
    {
        var reader = new FakeReader { Reading = new MachineResourceReading(800, 1000, 8 * Gigabyte, 2 * Gigabyte) };
        var sampler = new MachineResourceSampler(reader);

        var load = sampler.Sample(Seconds(0));

        // CPU time since boot says nothing about now.
        await Assert.That(load.CpuUtilization).IsNull();
        await Assert.That(load.MemoryUsed).IsEqualTo(0.75);
    }

    [Test]
    public async Task Cpu_Utilization_Is_The_Busy_Share_Since_The_Previous_Sample()
    {
        var reader = new FakeReader { Reading = new MachineResourceReading(800, 1000, 8 * Gigabyte, 2 * Gigabyte) };
        var sampler = new MachineResourceSampler(reader);
        sampler.Sample(Seconds(0));

        reader.Reading = new MachineResourceReading(825, 1100, 8 * Gigabyte, 2 * Gigabyte);
        var load = sampler.Sample(Seconds(1));

        await Assert.That(load.CpuUtilization).IsEqualTo(0.75);
    }

    [Test]
    // The operating system has not advanced its counters since the previous sample.
    [Arguments(800L, 1000L)]
    // A counter wrapped or was reset.
    [Arguments(10L, 20L)]
    [Arguments(900L, 1050L)]
    public async Task Counters_That_Give_No_Difference_Report_No_Cpu(long idle, long total)
    {
        var reader = new FakeReader { Reading = new MachineResourceReading(800, 1000, 8 * Gigabyte, 2 * Gigabyte) };
        var sampler = new MachineResourceSampler(reader);
        sampler.Sample(Seconds(0));

        reader.Reading = new MachineResourceReading(idle, total, 8 * Gigabyte, 2 * Gigabyte);
        var load = sampler.Sample(Seconds(1));

        await Assert.That(load.CpuUtilization).IsNull();
    }

    [Test]
    public async Task A_Failed_Read_Reports_Nothing_And_Starts_Over()
    {
        var reader = new FakeReader { Reading = new MachineResourceReading(800, 1000, 8 * Gigabyte, 2 * Gigabyte) };
        var sampler = new MachineResourceSampler(reader);
        sampler.Sample(Seconds(0));

        reader.Fails = true;
        await Assert.That(sampler.Sample(Seconds(1))).IsEqualTo(default(MachineLoad));

        reader.Fails = false;
        reader.Reading = new MachineResourceReading(900, 1200, 8 * Gigabyte, 2 * Gigabyte);
        await Assert.That(sampler.Sample(Seconds(2)).CpuUtilization).IsNull();
    }

    [Test]
    // 100 ms of a 1 s sample is the threshold.
    [Arguments(100_000L, true)]
    [Arguments(99_999L, false)]
    [Arguments(0L, false)]
    public async Task Throttling_Counts_Once_It_Covers_A_Tenth_Of_The_Sample(long throttledMicroseconds, bool stalled)
    {
        var reader = new FakeReader { Reading = Reading(throttled: 5_000_000) };
        var sampler = new MachineResourceSampler(reader);
        sampler.Sample(Seconds(0));

        reader.Reading = Reading(throttled: 5_000_000 + throttledMicroseconds);
        var load = sampler.Sample(Seconds(1));

        await Assert.That(load.CpuStalled).IsEqualTo(stalled);
        await Assert.That(load.MemoryStalled).IsFalse();
    }

    [Test]
    public async Task Cpu_And_Memory_Stalls_Are_Reported_Separately()
    {
        var reader = new FakeReader { Reading = Reading(cpuStall: 1_000, memoryStall: 1_000) };
        var sampler = new MachineResourceSampler(reader);
        sampler.Sample(Seconds(0));

        reader.Reading = Reading(cpuStall: 1_000, memoryStall: 501_000);
        var load = sampler.Sample(Seconds(1));

        await Assert.That(load.CpuStalled).IsFalse();
        await Assert.That(load.MemoryStalled).IsTrue();
    }

    [Test]
    public async Task Totals_Since_Boot_Are_Not_A_Stall()
    {
        // The counter was unreadable on the previous sample, so its whole history arrives at once.
        var reader = new FakeReader { Reading = Reading() };
        var sampler = new MachineResourceSampler(reader);
        sampler.Sample(Seconds(0));

        reader.Reading = Reading(throttled: 90_000_000, cpuStall: 90_000_000, memoryStall: 90_000_000);
        var load = sampler.Sample(Seconds(1));

        await Assert.That(load.CpuStalled).IsFalse();
        await Assert.That(load.MemoryStalled).IsFalse();
    }

    [Test]
    public async Task This_Machine_Gives_A_Sane_Reading()
    {
        var sampler = MachineResourceSampler.Create();
        if (sampler is null)
        {
            Skip.Test("This operating system has no machine resource reader");
        }

        var load = sampler.Sample();

        // The operating system advances its CPU counters at its own pace: macOS can go a
        // second without, and a sample in between has nothing to report.
        for (var attempt = 0; attempt < 50 && load.CpuUtilization is null; attempt++)
        {
            await Task.Delay(200);
            load = sampler.Sample();
        }

        await Assert.That(load.CpuUtilization).IsNotNull();
        await Assert.That(load.CpuUtilization!.Value).IsBetween(0, 1);
        await Assert.That(load.MemoryUsed).IsNotNull();
        await Assert.That(load.MemoryUsed!.Value).IsBetween(0.01, 1);
    }

    private static MachineResourceReading Reading(long? throttled = null, long? cpuStall = null, long? memoryStall = null)
        => new(800, 1000, 8 * Gigabyte, 2 * Gigabyte, throttled, cpuStall, memoryStall);

    private static long Seconds(int seconds) => 1 + seconds * Stopwatch.Frequency;

    private sealed class FakeReader : IMachineResourceReader
    {
        public MachineResourceReading Reading { get; set; }

        public bool Fails { get; set; }

        public bool TryRead(out MachineResourceReading reading)
        {
            reading = Reading;
            return !Fails;
        }
    }
}
#endif
