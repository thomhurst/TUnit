using System.Collections.Concurrent;
using System.Diagnostics;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Attribute that enables resource leak detection for a test or test class.
/// When applied, tracks thread pool threads and active timers before and after test execution,
/// logging a warning if significant resource growth is detected.
/// </summary>
/// <remarks>
/// This attribute is opt-in and should be applied to individual tests or test classes
/// where leak detection is desired. It implements both <see cref="ITestStartEventReceiver"/>
/// and <see cref="ITestEndEventReceiver"/> to capture resource snapshots around test execution.
/// <para>
/// The default threshold for reporting is 10 threads or 10 timers leaked during a single test.
/// You can customize this via the <see cref="ThreadThreshold"/> and <see cref="TimerThreshold"/> properties.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Test]
/// [DetectLeaks]
/// public async Task MyTest()
/// {
///     // If this test leaks 10+ threads, a warning will appear in test output
/// }
///
/// [DetectLeaks(ThreadThreshold = 5)]
/// public class MyTestClass
/// {
///     [Test]
///     public async Task MyTest() { }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class DetectLeaksAttribute : TUnitAttribute, ITestStartEventReceiver, ITestEndEventReceiver
{
    private static readonly ConcurrentDictionary<string, ResourceSnapshot> Snapshots = new();

    /// <summary>
    /// The minimum number of additional threads detected after a test to trigger a warning.
    /// Default is 10.
    /// </summary>
    public int ThreadThreshold { get; set; } = 10;

    /// <summary>
    /// The minimum number of additional active timers detected after a test to trigger a warning.
    /// Only available on .NET 6.0+. Default is 10.
    /// </summary>
    public int TimerThreshold { get; set; } = 10;

    /// <inheritdoc />
    public int Order => int.MaxValue; // Run after other receivers

    /// <inheritdoc />
    public ValueTask OnTestStart(TestContext context)
    {
        var snapshot = ResourceSnapshot.Capture();
        Snapshots[context.Id] = snapshot;
        return default;
    }

    /// <inheritdoc />
    public ValueTask OnTestEnd(TestContext context)
    {
        if (!Snapshots.TryRemove(context.Id, out var before))
        {
            return default;
        }

        var after = ResourceSnapshot.Capture();

        var threadDelta = after.AvailableWorkerThreads < before.AvailableWorkerThreads
            ? before.AvailableWorkerThreads - after.AvailableWorkerThreads
            : 0;

        var completionPortDelta = after.AvailableCompletionPortThreads < before.AvailableCompletionPortThreads
            ? before.AvailableCompletionPortThreads - after.AvailableCompletionPortThreads
            : 0;

        var timerDelta = after.ActiveTimerCount - before.ActiveTimerCount;

        var hasLeaks = false;
        var testName = context.Metadata.TestDetails.TestName;

        if (threadDelta >= ThreadThreshold)
        {
            context.Output.WriteError(
                $"[LeakDetection] Test '{testName}' may be leaking thread pool worker threads. " +
                $"Available workers decreased by {threadDelta} during execution " +
                $"(before: {before.AvailableWorkerThreads}, after: {after.AvailableWorkerThreads}).");
            hasLeaks = true;
        }

        if (completionPortDelta >= ThreadThreshold)
        {
            context.Output.WriteError(
                $"[LeakDetection] Test '{testName}' may be leaking I/O completion port threads. " +
                $"Available decreased by {completionPortDelta} during execution " +
                $"(before: {before.AvailableCompletionPortThreads}, after: {after.AvailableCompletionPortThreads}).");
            hasLeaks = true;
        }

        if (timerDelta >= TimerThreshold)
        {
            context.Output.WriteError(
                $"[LeakDetection] Test '{testName}' may be leaking timers. " +
                $"Active timers increased by {timerDelta} during execution " +
                $"(before: {before.ActiveTimerCount}, after: {after.ActiveTimerCount}).");
            hasLeaks = true;
        }

        if (!hasLeaks)
        {
            return default;
        }

        context.Output.WriteError(
            $"[LeakDetection] Consider ensuring all background tasks, timers, and thread pool work " +
            $"are properly awaited or disposed in test '{testName}'.");

        return default;
    }

    /// <summary>
    /// Captures a snapshot of key resource metrics at a point in time.
    /// </summary>
    internal readonly struct ResourceSnapshot
    {
        public int AvailableWorkerThreads { get; init; }
        public int AvailableCompletionPortThreads { get; init; }
        public long ActiveTimerCount { get; init; }

        public static ResourceSnapshot Capture()
        {
            ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);

            long activeTimerCount = 0;
#if NET6_0_OR_GREATER
            activeTimerCount = Timer.ActiveCount;
#endif

            return new ResourceSnapshot
            {
                AvailableWorkerThreads = workerThreads,
                AvailableCompletionPortThreads = completionPortThreads,
                ActiveTimerCount = activeTimerCount,
            };
        }
    }
}
