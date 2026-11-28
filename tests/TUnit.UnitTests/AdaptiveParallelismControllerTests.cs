#if NET
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

        // One summary for the two calls. Its wording depends on whether the host was under
        // enough pressure to move the limit while the controller ran.
        var summaries = logger.Messages.Where(x => x.Level == LogLevel.Information).Select(x => x.Message).ToArray();
        await Assert.That(summaries).HasSingleItem();
        await Assert.That(summaries[0]).StartsWith("Adaptive parallelism: the parallel test limit ");
    }

    [Test]
    public async Task Runtime_Memory_Load_Is_A_Sane_Ratio()
    {
        // The runtime fills these numbers in at the first garbage collection.
        GC.Collect();

        await Assert.That(AdaptiveParallelismController.ReadRuntimeMemoryLoad()).IsBetween(0, 2);
    }

    [Test]
    [Arguments(null, 8, 16, 4, 64)]
    // A configured maximum is where a fixed run would be, so it is the start and the upper bound.
    [Arguments(100, 8, 100, 4, 100)]
    [Arguments(4, 8, 4, 1, 4)]
    [Arguments(1, 8, 1, 1, 1)]
    [Arguments(null, 1, 2, 1, 8)]
    public async Task Adaptive_Limits_Follow_The_Configured_Maximum(int? configured, int processorCount, int initial, int minimum, int maximum)
    {
        var limits = TUnit.Engine.Scheduling.TestScheduler.GetAdaptiveLimits(configured, processorCount);

        await Assert.That(limits).IsEqualTo(new ParallelismLimits(initial, minimum, maximum));
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
