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
    public async Task Cpu_Saturation_Lowers_The_Limit_Only_When_Sustained_And_Work_Is_Queuing()
    {
        var simulation = new Simulation(Limits)
        {
            CpuUtilization = 0.99,
            QueueDelay = TimeSpan.FromMilliseconds(150),
        };

        simulation.Step();
        await Assert.That(simulation.Changes).IsEmpty();

        simulation.Step();
        await Assert.That(simulation.Changes).IsEquivalentTo([new AdaptiveDecision(12, AdaptiveLimitChange.CpuSaturation)]);
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
            simulation.Step();

            await Assert.That(simulation.Policy.Limit).IsBetween(Limits.Minimum, Limits.Maximum);
        }
    }

    private sealed class Simulation(ParallelismLimits limits)
    {
        private static readonly TimeSpan SampleInterval = TimeSpan.FromMilliseconds(250);
        private double _partialTests;

        public AdaptiveParallelismPolicy Policy { get; } = new(limits);

        public List<AdaptiveDecision> Changes { get; } = [];

        public Func<int, double> Throughput { get; init; } = limit => limit * TestsPerSecondPerSlot;

        public double CpuUtilization { get; set; } = 0.2;

        public double MemoryLoad { get; set; } = 0.3;

        public TimeSpan QueueDelay { get; set; }

        public bool LimitReached { get; set; } = true;

        public bool Step()
        {
            _partialTests += Throughput(Policy.Limit) * SampleInterval.TotalSeconds;
            var completed = (long) _partialTests;
            _partialTests -= completed;

            var decision = Policy.OnSample(new AdaptiveSample(SampleInterval, CpuUtilization, MemoryLoad, QueueDelay, completed, LimitReached));

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
