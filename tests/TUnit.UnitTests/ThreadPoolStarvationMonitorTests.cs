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

    private int _minWorkerThreads;
    private int _minIoThreads;

    [Before(HookType.Test)]
    public void SnapshotMinimum() => ThreadPool.GetMinThreads(out _minWorkerThreads, out _minIoThreads);

    [After(HookType.Test)]
    public void RestoreMinimum() => ThreadPool.SetMinThreads(_minWorkerThreads, _minIoThreads);

    private static int CurrentMinWorkerThreads()
    {
        ThreadPool.GetMinThreads(out var workerThreads, out _);
        return workerThreads;
    }

    private static ThreadPoolStarvationMonitor StartMonitor()
        => ThreadPoolStarvationMonitor.Start(MaxParallelism, new NullLogger())
           ?? throw new InvalidOperationException("Monitor did not start");

    [Test]
    public async Task Raise_Sets_Minimum_To_Cap_And_Dispose_Restores_It()
    {
        var monitor = StartMonitor();

        monitor.Raise();
        await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(MaxParallelism + Environment.ProcessorCount);

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

        monitor.Raise();
        monitor.Dispose();

        await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(_minWorkerThreads + 5);
    }

    [Test]
    public async Task Dispose_Keeps_Minimum_User_Changed_After_Raise()
    {
        var monitor = StartMonitor();
        monitor.Raise();
        ThreadPool.SetMinThreads(_minWorkerThreads + 7, _minIoThreads);

        monitor.Dispose();

        await Assert.That(CurrentMinWorkerThreads()).IsEqualTo(_minWorkerThreads + 7);
    }

    [Test]
    public async Task Does_Not_Start_When_Minimum_Already_At_Cap()
    {
        ThreadPool.SetMinThreads(MaxParallelism + Environment.ProcessorCount, _minIoThreads);

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
    [Arguments(60, 0.1, true)]
    [Arguments(50, 0.49, true)]
    [Arguments(10, 0.1, false)]
    [Arguments(60, 0.9, false)]
    public async Task Starved_Only_When_Probe_Waits_And_Cpus_Are_Idle(int probeWaitMs, double cpuUtilization, bool expected)
    {
        await Assert.That(ThreadPoolStarvationMonitor.IsStarved(TimeSpan.FromMilliseconds(probeWaitMs), cpuUtilization))
            .IsEqualTo(expected);
    }

    [Test]
    [Arguments(10, 0, false)]
    [Arguments(10, 9, false)]
    [Arguments(10, 10, true)]
    [Arguments(10, 500, true)]
    public async Task Detects_Saturated_Thread_Pool(int threadCount, long pendingWorkItems, bool expected)
    {
        await Assert.That(TimeoutDiagnostics.IsThreadPoolSaturated(threadCount, pendingWorkItems)).IsEqualTo(expected);
    }

    [Test]
    public async Task Timeout_Diagnostics_Include_Thread_Pool_Snapshot_And_Saturation_Hint()
    {
        var sb = new StringBuilder();
        TimeoutDiagnostics.AppendThreadPoolDiagnostics(sb, threadCount: 4, pendingWorkItems: 50);
        var message = sb.ToString();

        await Assert.That(message).Contains("--- Thread Pool ---");
        await Assert.That(message).Contains("Threads: 4, pending work items: 50");
        await Assert.That(message).Contains("thread pool appears saturated");
    }

    [Test]
    public async Task Timeout_Diagnostics_Omit_Saturation_Hint_When_Pool_Is_Idle()
    {
        var sb = new StringBuilder();
        TimeoutDiagnostics.AppendThreadPoolDiagnostics(sb, threadCount: 4, pendingWorkItems: 0);

        await Assert.That(sb.ToString()).DoesNotContain("saturated");
    }
}
#endif
