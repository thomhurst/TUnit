using TUnit.Engine.Scheduling;

namespace TUnit.UnitTests;

public class AdaptiveParallelismPolicyTests
{
    private static readonly ParallelismLimits Limits = new(Initial: 16, Minimum: 4, Maximum: 64);

    // 100 ms tests: each slot completes ten tests per second.
    private const double TestsPerSecondPerSlot = 10;

    [Test]
    public async Task Raises_To_The_Maximum_While_Throughput_Follows_The_Limit()
    {
        var simulation = new Simulation(Limits);

        simulation.Run(TimeSpan.FromSeconds(60));

        await Assert.That(simulation.Policy.Limit).IsEqualTo(Limits.Maximum);
        await Assert.That(simulation.Changes.Select(x => x.Change)).DoesNotContain(AdaptiveLimitChange.ProbeReverted);
    }

    [Test]
    public async Task Reverts_A_Raise_That_Brings_No_Throughput()
    {
        // Something outside the process serves 16 tests at a time, whatever the limit.
        var simulation = new Simulation(Limits)
        {
            Throughput = limit => Math.Min(limit, 16) * TestsPerSecondPerSlot,
        };

        simulation.Run(TimeSpan.FromSeconds(10));

        await Assert.That(simulation.Changes).IsEquivalentTo(
        [
            new AdaptiveDecision(24, AdaptiveLimitChange.Probe),
            new AdaptiveDecision(16, AdaptiveLimitChange.ProbeReverted),
        ]);
    }

    [Test]
    public async Task Waits_Longer_Before_Each_Probe_After_A_Failed_One()
    {
        var simulation = new Simulation(Limits)
        {
            Throughput = limit => Math.Min(limit, 16) * TestsPerSecondPerSlot,
        };

        simulation.Run(TimeSpan.FromSeconds(60));

        // Probes at about 1 s, 18 s (15 s hold) and 51 s (30 s hold).
        await Assert.That(simulation.Changes.Count(x => x.Change == AdaptiveLimitChange.Probe)).IsEqualTo(3);
        await Assert.That(simulation.Changes.Max(x => x.Limit)).IsEqualTo(24);
    }

    [Test]
    public async Task Does_Not_Raise_Without_Cpu_Headroom()
    {
        var simulation = new Simulation(Limits) { CpuUtilization = 0.9 };

        simulation.Run(TimeSpan.FromSeconds(30));

        await Assert.That(simulation.Changes).IsEmpty();
    }

    [Test]
    public async Task Does_Not_Raise_Without_Memory_Headroom()
    {
        var simulation = new Simulation(Limits) { MemoryLoad = 0.9 };

        simulation.Run(TimeSpan.FromSeconds(30));

        await Assert.That(simulation.Changes).IsEmpty();
    }

    [Test]
    public async Task Does_Not_Raise_While_Work_Queues_For_A_Thread()
    {
        var simulation = new Simulation(Limits) { QueueDelay = TimeSpan.FromMilliseconds(200) };

        simulation.Run(TimeSpan.FromSeconds(30));

        await Assert.That(simulation.Changes).IsEmpty();
    }

    [Test]
    public async Task Does_Not_Raise_When_The_Limit_Is_Not_What_Holds_Tests_Back()
    {
        var simulation = new Simulation(Limits) { LimitReached = false };

        simulation.Run(TimeSpan.FromSeconds(30));

        await Assert.That(simulation.Changes).IsEmpty();
    }

    [Test]
    public async Task Does_Not_Raise_When_Tests_Are_Too_Slow_To_Measure()
    {
        // One test every ten seconds: three in the longest window.
        var simulation = new Simulation(Limits) { Throughput = _ => 0.1 };

        simulation.Run(TimeSpan.FromMinutes(2));

        await Assert.That(simulation.Changes).IsEmpty();
    }

    [Test]
    public async Task Memory_Pressure_Halves_The_Limit_Then_Waits_For_It_To_Take_Effect()
    {
        var simulation = new Simulation(Limits) { MemoryLoad = 1.05 };

        simulation.Run(TimeSpan.FromSeconds(2));

        await Assert.That(simulation.Changes).IsEquivalentTo([new AdaptiveDecision(8, AdaptiveLimitChange.MemoryPressure)]);
    }

    [Test]
    public async Task Sustained_Memory_Pressure_Stops_At_The_Minimum()
    {
        var simulation = new Simulation(Limits) { MemoryLoad = 1.05 };

        simulation.Run(TimeSpan.FromSeconds(30));

        await Assert.That(simulation.Changes).IsEquivalentTo(
        [
            new AdaptiveDecision(8, AdaptiveLimitChange.MemoryPressure),
            new AdaptiveDecision(4, AdaptiveLimitChange.MemoryPressure),
        ]);
    }

    [Test]
    public async Task Returns_To_The_Starting_Limit_In_Steps_After_Pressure_Eases()
    {
        var simulation = new Simulation(Limits) { MemoryLoad = 1.05 };
        simulation.Run(TimeSpan.FromSeconds(1));

        simulation.MemoryLoad = 0.3;
        simulation.Run(TimeSpan.FromSeconds(10));

        // Halfway back at first: the limit that caused pressure is off limits while it is recent.
        await Assert.That(simulation.Changes).IsEquivalentTo(
        [
            new AdaptiveDecision(8, AdaptiveLimitChange.MemoryPressure),
            new AdaptiveDecision(12, AdaptiveLimitChange.Recovery),
        ]);

        simulation.Run(TimeSpan.FromSeconds(10));

        await Assert.That(simulation.Changes[2]).IsEqualTo(new AdaptiveDecision(16, AdaptiveLimitChange.Recovery));
    }

    [Test]
    public async Task Cpu_Saturation_Lowers_The_Limit_Once_Throughput_Has_Been_Measured()
    {
        var simulation = new Simulation(Limits)
        {
            CpuUtilization = 0.99,
            QueueDelay = TimeSpan.FromMilliseconds(150),
        };

        // The first window measures what the lowering will be judged against.
        simulation.Run(TimeSpan.FromSeconds(1));
        await Assert.That(simulation.Changes).IsEmpty();

        simulation.Step();
        await Assert.That(simulation.Changes).IsEquivalentTo([new AdaptiveDecision(12, AdaptiveLimitChange.CpuSaturation)]);
    }

    [Test]
    public async Task Keeps_Lowering_For_Cpu_Saturation_While_Throughput_Holds()
    {
        // Four CPUs' worth of work, whatever the limit: fewer tests at once lose nothing.
        var simulation = new Simulation(Limits)
        {
            CpuUtilization = 0.99,
            QueueDelay = TimeSpan.FromMilliseconds(150),
            Throughput = _ => 4 * TestsPerSecondPerSlot,
        };

        simulation.Run(TimeSpan.FromSeconds(30));

        await Assert.That(simulation.Changes.Select(x => x.Change)).DoesNotContain(AdaptiveLimitChange.LowerReverted);
        await Assert.That(simulation.Policy.Limit).IsEqualTo(Limits.Minimum);
    }

    [Test]
    public async Task Does_Not_Lower_For_Cpu_Saturation_Below_The_Processor_Count()
    {
        // Twelve processors: with twelve tests running, this process no longer oversubscribes them.
        var simulation = new Simulation(Limits, processorCount: 12)
        {
            CpuUtilization = 0.99,
            QueueDelay = TimeSpan.FromMilliseconds(150),
            CpuStalled = true,
            Throughput = _ => 12 * TestsPerSecondPerSlot,
        };

        simulation.Run(TimeSpan.FromSeconds(60));

        await Assert.That(simulation.Changes.Min(x => x.Limit)).IsEqualTo(12);
    }

    [Test]
    public async Task Reverts_A_Lowering_For_Cpu_Saturation_That_Costs_Throughput()
    {
        // Tests that mostly wait: other processes keep the CPUs busy, and every slot taken
        // away is throughput lost.
        var simulation = new Simulation(Limits)
        {
            CpuUtilization = 0.99,
            QueueDelay = TimeSpan.FromMilliseconds(150),
        };

        simulation.Run(TimeSpan.FromSeconds(10));

        await Assert.That(simulation.Changes).IsEquivalentTo(
        [
            new AdaptiveDecision(12, AdaptiveLimitChange.CpuSaturation),
            new AdaptiveDecision(16, AdaptiveLimitChange.LowerReverted),
        ]);
    }

    [Test]
    public async Task Waits_Longer_Before_Each_Lowering_After_A_Failed_One()
    {
        var simulation = new Simulation(Limits)
        {
            CpuUtilization = 0.99,
            QueueDelay = TimeSpan.FromMilliseconds(150),
        };

        simulation.Run(TimeSpan.FromSeconds(60));

        // Lowered at about 1 s, 19 s (15 s hold) and 52 s (30 s hold), and put back each time.
        await Assert.That(simulation.Changes.Count(x => x.Change == AdaptiveLimitChange.CpuSaturation)).IsEqualTo(3);
        await Assert.That(simulation.Changes.Min(x => x.Limit)).IsEqualTo(12);
    }

    [Test]
    public async Task Stalled_Tasks_Lower_The_Limit_Whatever_The_Utilization()
    {
        // A container at its CPU quota on a host that is otherwise idle.
        var simulation = new Simulation(Limits)
        {
            CpuUtilization = 0.3,
            CpuStalled = true,
            Throughput = _ => 4 * TestsPerSecondPerSlot,
        };

        simulation.Run(TimeSpan.FromSeconds(3));

        await Assert.That(simulation.Changes).IsEquivalentTo([new AdaptiveDecision(12, AdaptiveLimitChange.CpuSaturation)]);
    }

    [Test]
    public async Task Does_Not_Raise_While_Tasks_Are_Stalled()
    {
        // Lowering is put back because throughput follows the limit, and nothing is raised.
        var simulation = new Simulation(Limits) { CpuStalled = true };

        simulation.Run(TimeSpan.FromSeconds(30));

        await Assert.That(simulation.Changes.Select(x => x.Change)).DoesNotContain(AdaptiveLimitChange.Probe);
        await Assert.That(simulation.Changes.Max(x => x.Limit)).IsEqualTo(Limits.Initial);
    }

    [Test]
    public async Task Several_Lowerings_Cannot_Each_Give_A_Little_Throughput_Away()
    {
        // Each step down costs 5%: within the allowance alone, beyond it together.
        var simulation = new Simulation(Limits)
        {
            CpuUtilization = 0.99,
            QueueDelay = TimeSpan.FromMilliseconds(150),
            Throughput = limit => limit switch
            {
                >= 16 => 100,
                >= 12 => 95,
                _ => 90,
            },
        };

        simulation.Run(TimeSpan.FromSeconds(12));

        await Assert.That(simulation.Changes).IsEquivalentTo(
        [
            new AdaptiveDecision(12, AdaptiveLimitChange.CpuSaturation),
            new AdaptiveDecision(9, AdaptiveLimitChange.CpuSaturation),
            new AdaptiveDecision(12, AdaptiveLimitChange.LowerReverted),
        ]);

        // Retries of the rejected step, and raises that are tried and put back in between,
        // are still judged against the throughput before the first lowering.
        simulation.Run(TimeSpan.FromMinutes(10));

        await Assert.That(simulation.Policy.Limit).IsEqualTo(12);
        await Assert.That(simulation.Changes.Count(x => x.Limit == 9))
            .IsEqualTo(simulation.Changes.Count(x => x.Change == AdaptiveLimitChange.LowerReverted));
    }

    [Test]
    public async Task Tries_The_Way_Back_To_The_Starting_Limit_Without_Cpu_Headroom()
    {
        var simulation = new Simulation(Limits) { MemoryLoad = 1.05 };
        simulation.Run(TimeSpan.FromSeconds(1));

        // Memory has eased, but other processes keep the CPUs busy.
        simulation.MemoryLoad = 0.3;
        simulation.CpuUtilization = 0.9;
        simulation.Run(TimeSpan.FromSeconds(40));

        // No free recovery without headroom: each step back is a trial, kept because
        // throughput follows the limit. The first waits for the hold after the pressure.
        await Assert.That(simulation.Changes.Take(3)).IsEquivalentTo(
        [
            new AdaptiveDecision(8, AdaptiveLimitChange.MemoryPressure),
            new AdaptiveDecision(12, AdaptiveLimitChange.Probe),
            new AdaptiveDecision(16, AdaptiveLimitChange.Probe),
        ]);
        await Assert.That(simulation.Policy.Limit).IsEqualTo(16);
    }

    [Test]
    public async Task Busy_Cpus_Alone_Do_Not_Lower_The_Limit()
    {
        var simulation = new Simulation(Limits) { CpuUtilization = 0.99 };

        simulation.Run(TimeSpan.FromSeconds(30));

        await Assert.That(simulation.Changes).IsEmpty();
    }

    [Test]
    public async Task Memory_Pressure_During_A_Probe_Lowers_From_The_Probed_Limit()
    {
        var simulation = new Simulation(Limits);
        simulation.RunUntilChange();
        await Assert.That(simulation.Changes[0]).IsEqualTo(new AdaptiveDecision(24, AdaptiveLimitChange.Probe));

        simulation.MemoryLoad = 1.05;
        simulation.Step();

        await Assert.That(simulation.Changes[1]).IsEqualTo(new AdaptiveDecision(12, AdaptiveLimitChange.MemoryPressure));
    }

    [Test]
    public async Task Limit_Stays_Within_Its_Bounds_Under_Changing_Load()
    {
        var simulation = new Simulation(Limits);
        var random = new Random(7871);

        for (var i = 0; i < 2000; i++)
        {
            simulation.CpuUtilization = random.NextDouble();
            simulation.MemoryLoad = random.NextDouble() * 1.2;
            simulation.QueueDelay = TimeSpan.FromMilliseconds(random.Next(0, 300));
            simulation.LimitReached = random.Next(4) != 0;
            simulation.CpuStalled = random.Next(5) == 0;
            simulation.Step();

            await Assert.That(simulation.Policy.Limit).IsBetween(Limits.Minimum, Limits.Maximum);
        }
    }

    private sealed class Simulation(ParallelismLimits limits, int processorCount = 4)
    {
        private static readonly TimeSpan SampleInterval = TimeSpan.FromMilliseconds(250);
        private double _partialTests;

        public AdaptiveParallelismPolicy Policy { get; } = new(limits, processorCount);

        public List<AdaptiveDecision> Changes { get; } = [];

        public Func<int, double> Throughput { get; init; } = limit => limit * TestsPerSecondPerSlot;

        public double CpuUtilization { get; set; } = 0.2;

        public double MemoryLoad { get; set; } = 0.3;

        public TimeSpan QueueDelay { get; set; }

        public bool LimitReached { get; set; } = true;

        public bool CpuStalled { get; set; }

        public bool Step()
        {
            _partialTests += Throughput(Policy.Limit) * SampleInterval.TotalSeconds;
            var completed = (long) _partialTests;
            _partialTests -= completed;

            var decision = Policy.OnSample(new AdaptiveSample(SampleInterval, CpuUtilization, MemoryLoad, QueueDelay, completed, LimitReached, CpuStalled));

            if (decision.Change == AdaptiveLimitChange.None)
            {
                return false;
            }

            Changes.Add(decision);
            return true;
        }

        public void Run(TimeSpan duration)
        {
            for (var elapsed = TimeSpan.Zero; elapsed < duration; elapsed += SampleInterval)
            {
                Step();
            }
        }

        public void RunUntilChange()
        {
            for (var i = 0; i < 1000 && !Step(); i++)
            {
            }
        }
    }
}
