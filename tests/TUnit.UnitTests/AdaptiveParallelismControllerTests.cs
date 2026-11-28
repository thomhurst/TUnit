#if NET
using System.Text;
using TUnit.Core.Logging;
using TUnit.Engine.Scheduling;

namespace TUnit.UnitTests;

public class AdaptiveParallelismControllerTests
{
    [Test]
    public async Task Does_Not_Start_For_A_Fixed_Limit()
    {
        using var controller = AdaptiveParallelismController.Start(new MaxParallelismLimiter(() => 4), new NullLogger());

        await Assert.That(controller).IsNull();
    }

    [Test]
    public async Task Reports_The_Limit_When_It_Stops()
    {
        var logger = new RecordingLogger();
        var limiter = new MaxParallelismLimiter(() => new ParallelismLimits(Initial: 4, Minimum: 1, Maximum: 8));

        var controller = AdaptiveParallelismController.Start(limiter, logger);
        await Assert.That(controller).IsNotNull();

        controller!.Dispose();
        controller.Dispose();

        await Assert.That(logger.Messages.Where(x => x.Level == LogLevel.Information).Select(x => x.Message))
            .IsEquivalentTo(["Adaptive parallelism: the parallel test limit stayed at 4"]);
    }

    [Test]
    public async Task Memory_Load_Is_A_Sane_Ratio()
    {
        // The runtime fills these numbers in at the first garbage collection.
        GC.Collect();

        await Assert.That(AdaptiveParallelismController.ReadMemoryLoad()).IsBetween(0, 2);
    }

    [Test]
    [Arguments(null, 8, 16, 4, 64)]
    // A configured maximum is a hard upper bound, above or below the starting limit.
    [Arguments(100, 8, 16, 4, 100)]
    [Arguments(4, 8, 4, 1, 4)]
    [Arguments(1, 8, 1, 1, 1)]
    [Arguments(null, 1, 2, 1, 8)]
    public async Task Adaptive_Limits_Follow_The_Configured_Maximum(int? configured, int processorCount, int initial, int minimum, int maximum)
    {
        var limits = TUnit.Engine.Scheduling.TestScheduler.GetAdaptiveLimits(configured, processorCount);

        await Assert.That(limits).IsEqualTo(new ParallelismLimits(initial, minimum, maximum));
    }

    [Test]
    public async Task Machine_Cpu_Is_A_Share_Of_Capacity()
    {
        using var monitor = MachineCpuMonitor.Create();
        if (monitor is null)
        {
            Skip.Test("This operating system has no machine-wide CPU reading");
        }

        await Assert.That(monitor.Sample()).IsNull();

        // Long enough for the kernel's tick counters to advance.
        var spinUntil = DateTime.UtcNow.AddMilliseconds(300);
        while (DateTime.UtcNow < spinUntil)
        {
        }

        var utilization = monitor.Sample();
        await Assert.That(utilization).IsNotNull();
        await Assert.That(utilization!.Value).IsBetween(0, 1);
    }

    [Test]
    public async Task Proc_Stat_Counts_Idle_And_IoWait_As_Not_Busy()
    {
        // user nice system idle iowait irq softirq steal guest guest_nice
        var content = "cpu  100 20 30 800 40 5 3 2 50 0\ncpu0 50 10 15 400 20 2 1 1 25 0\n"u8;

        var parsed = MachineCpuMonitor.TryParseProcStat(content, out var busy, out var total);

        await Assert.That(parsed).IsTrue();
        await Assert.That(total).IsEqualTo(1000UL);
        await Assert.That(busy).IsEqualTo(160UL);
    }

    [Test]
    [Arguments("")]
    [Arguments("cpu0 1 2 3 4\n")]
    [Arguments("cpu  1 2 3\n")]
    [Arguments("cpu  1 x 3 4\n")]
    public async Task Proc_Stat_Rejects_Content_It_Cannot_Read(string content)
    {
        var parsed = MachineCpuMonitor.TryParseProcStat(Encoding.ASCII.GetBytes(content), out _, out _);

        await Assert.That(parsed).IsFalse();
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly List<(LogLevel Level, string Message)> _messages = [];

        public (LogLevel Level, string Message)[] Messages
        {
            get
            {
                lock (_messages)
                {
                    return [.. _messages];
                }
            }
        }

        public ValueTask LogAsync<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Log(logLevel, state, exception, formatter);
            return default;
        }

        public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_messages)
            {
                _messages.Add((logLevel, formatter(state, exception)));
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;
    }
}
#endif
