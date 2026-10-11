#if NET
using System.Text;
using TUnit.Engine.Helpers;
using TUnit.Core.Logging;
using TUnit.Engine.Configuration;
using TUnit.Engine.Scheduling;

namespace TUnit.UnitTests;

// [NotInParallel] because tests change the process-wide thread pool minimum.
[NotInParallel]
public class ThreadPoolStarvationMonitorTests
{
    private const int MaxParallelism = 200;

    // Keeps the first raise target (32 per processor) below the escalation ceiling (64 per
    // processor) on any core count. With MaxParallelism, a runner with 3 or fewer processors
    // clamps the first target to the ceiling, so there is nothing left to escalate.
    private static int EscalationParallelism => Environment.ProcessorCount * 31;

    private int _minWorkerThreads;
    private int _minIoThreads;

    [Before(HookType.Test)]
    public void SnapshotMinimum() => ThreadPool.GetMinThreads(out _minWorkerThreads, out _minIoThreads);

    [After(HookType.Test)]
    public void RestoreMinimum() => ThreadPool.SetMinThreads(_minWorkerThreads, _minIoThreads);

    private static int Cap => ComputeCap(MaxParallelism);

    private static int ComputeCap(int maxParallelism) => ThreadPoolStarvationMonitor.ComputeMaxWorkerThreads(maxParallelism, Environment.ProcessorCount);

    private static int CurrentMinWorkerThreads()
    {
        ThreadPool.GetMinThreads(out var workerThreads, out _);
        return workerThreads;
    }

    private static ThreadPoolStarvationMonitor StartMonitor(int maxParallelism = MaxParallelism)
        => ThreadPoolStarvationMonitor.Start(maxParallelism, new NullLogger())
           ?? throw new InvalidOperationException("Monitor did not start");

    [Test]
    public async Task Raise_Sets_Minimum_To_Cap_And_Dispose_Restores_It()
    {
        var monitor = StartMonitor();

        monitor.TryRaise();
        await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(Cap);

        monitor.Dispose();
        await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(_minWorkerThreads);
    }

    [Test]
    public async Task Dispose_Without_Raise_Leaves_Minimum_Unchanged()
    {
        StartMonitor().Dispose();

        await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(_minWorkerThreads);
    }

    [Test]
    public async Task Dispose_Restores_Minimum_User_Set_After_Start()
    {
        var monitor = StartMonitor();
        ThreadPool.SetMinThreads(_minWorkerThreads + 5, _minIoThreads);

        monitor.TryRaise();
        monitor.Dispose();

        await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(_minWorkerThreads + 5);
    }

    [Test]
    public async Task Dispose_Keeps_Minimum_User_Changed_After_Raise()
    {
        var monitor = StartMonitor();
        monitor.TryRaise();
        ThreadPool.SetMinThreads(_minWorkerThreads + 7, _minIoThreads);

        monitor.Dispose();

        await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(_minWorkerThreads + 7);
    }

    [Test]
    public async Task Raise_Is_Clamped_To_Pool_Maximum()
    {
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxIoThreads);
        var lowerMaximum = _minWorkerThreads + 10;
        try
        {
            await Assert.That(lowerMaximum).IsLessThan(Cap);
            ThreadPool.SetMaxThreads(lowerMaximum, maxIoThreads);

            var monitor = StartMonitor();
            monitor.TryRaise();

            await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(lowerMaximum);
            monitor.Dispose();
        }
        finally
        {
            ThreadPool.SetMaxThreads(maxWorkerThreads, maxIoThreads);
        }
    }

    private static int Ceiling
    {
        get
        {
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);
            return Math.Max(Cap, Math.Min(Environment.ProcessorCount * ThreadPoolStarvationMonitor.MaxWorkerThreadsPerProcessor, maxWorkerThreads));
        }
    }

    [Test]
    public async Task Escalates_From_User_Minimum_Already_At_First_Target()
    {
        var cap = ComputeCap(EscalationParallelism);
        ThreadPool.SetMinThreads(cap, _minIoThreads);

        var monitor = StartMonitor(EscalationParallelism);
        try
        {
            monitor.TryRaise();

            await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(Math.Min(cap * 2, Ceiling));
        }
        finally
        {
            monitor.Dispose();
        }

        await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(cap);
    }

    [Test]
    public async Task Does_Not_Start_When_Minimum_Already_At_Ceiling()
    {
        ThreadPool.SetMinThreads(Ceiling, _minIoThreads);

        await Assert.That(ThreadPoolStarvationMonitor.Start(MaxParallelism, new NullLogger())).IsNull();
    }

    [Test]
    public async Task Does_Not_Start_When_Disabled_By_Environment_Variable()
    {
        var previous = Environment.GetEnvironmentVariable(EnvironmentConstants.DisableThreadPoolTuning);
        Environment.SetEnvironmentVariable(EnvironmentConstants.DisableThreadPoolTuning, "1");
        try
        {
            await Assert.That(ThreadPoolStarvationMonitor.Start(MaxParallelism, new NullLogger())).IsNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentConstants.DisableThreadPoolTuning, previous);
        }
    }

    [Test]
    public async Task Escalates_Only_After_Pool_Reaches_Raised_Minimum()
    {
        var cap = ComputeCap(EscalationParallelism);
        var monitor = StartMonitor(EscalationParallelism);
        try
        {
            await Assert.That(monitor.TryRaise()).IsTrue();
            await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(cap);

            // The idle pool has far fewer threads than the raised minimum, so a second
            // starved sample waits for it to catch up instead of stacking another raise.
            await Assert.That(monitor.TryRaise()).IsTrue();
            await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(cap);
        }
        finally
        {
            monitor.Dispose();
        }
    }

    [Test]
    public async Task Stops_Escalating_When_User_Changes_Minimum()
    {
        var monitor = StartMonitor();
        try
        {
            monitor.TryRaise();
            ThreadPool.SetMinThreads(_minWorkerThreads + 3, _minIoThreads);

            await Assert.That(monitor.TryRaise()).IsFalse();
            await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(_minWorkerThreads + 3);
        }
        finally
        {
            monitor.Dispose();
        }
    }

    [Test]
    [Arguments(100, 1280, 200)]
    [Arguments(1000, 1280, 1280)]
    [Arguments(1280, 1280, 1280)]
    public async Task Escalation_Doubles_Up_To_Ceiling(int raised, int ceiling, int expected)
    {
        await Assert.That(ThreadPoolStarvationMonitor.ComputeEscalatedMinimum(raised, ceiling)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("1", true)]
    [Arguments("TRUE", true)]
    [Arguments("yes", true)]
    [Arguments("0", false)]
    [Arguments("false", false)]
    [Arguments("", false)]
    [Arguments(null, false)]
    public async Task Environment_Flag_Accepts_Only_Truthy_Values(string? value, bool expected)
    {
        await Assert.That(EnvironmentFlags.IsTruthy(value)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(80, 20, 100)]
    [Arguments(10000, 20, 1280)]
    [Arguments(10000, 2, 128)]
    [Arguments(int.MaxValue, 8, 512)]
    public async Task Cap_Is_Parallelism_Plus_Processors_Clamped_Per_Processor(int maxParallelism, int processorCount, int expected)
    {
        await Assert.That(ThreadPoolStarvationMonitor.ComputeMaxWorkerThreads(maxParallelism, processorCount)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(60, 0.1, true)]
    [Arguments(50, 0.49, true)]
    [Arguments(45, 0.1, true)]
    [Arguments(30, 0.1, false)]
    [Arguments(10, 0.1, false)]
    [Arguments(60, 0.9, false)]
    public async Task Starved_Only_When_Probe_Waits_And_Cpus_Are_Idle(int probeWaitMs, double cpuUtilization, bool expected)
    {
        await Assert.That(ThreadPoolStarvationMonitor.IsStarved(TimeSpan.FromMilliseconds(probeWaitMs), cpuUtilization))
            .IsEqualTo(expected);
    }

    [Test]
    [Arguments(10, 10, 0, false)]
    [Arguments(10, 9, 500, false)]
    [Arguments(10, 10, 1, true)]
    [Arguments(10, 10, 500, true)]
    public async Task Detects_Saturated_Thread_Pool(int threadCount, int busyWorkerThreads, long pendingWorkItems, bool expected)
    {
        await Assert.That(TimeoutDiagnostics.IsThreadPoolSaturated(threadCount, busyWorkerThreads, pendingWorkItems)).IsEqualTo(expected);
    }

    [Test]
    public async Task Timeout_Diagnostics_Include_Thread_Pool_Snapshot_And_Saturation_Hint()
    {
        var sb = new StringBuilder();
        TimeoutDiagnostics.AppendThreadPoolDiagnostics(sb, threadCount: 4, busyWorkerThreads: 4, pendingWorkItems: 1);
        var message = sb.ToString();

        await Assert.That(message).Contains("--- Thread Pool ---");
        await Assert.That(message).Contains("Threads: 4, busy: 4, pending work items: 1");
        await Assert.That(message).Contains("thread pool appears saturated");
    }

    [Test]
    public async Task Timeout_Diagnostics_Omit_Saturation_Hint_When_Pool_Is_Idle()
    {
        var sb = new StringBuilder();
        TimeoutDiagnostics.AppendThreadPoolDiagnostics(sb, threadCount: 4, busyWorkerThreads: 1, pendingWorkItems: 0);

        await Assert.That(sb.ToString()).DoesNotContain("saturated");
    }
}
#endif
