#if NET
using System.Text;
using TUnit.Engine.Helpers;
using TUnit.Engine.Scheduling;

namespace TUnit.UnitTests;

public class ThreadPoolStarvationMonitorTests
{
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
