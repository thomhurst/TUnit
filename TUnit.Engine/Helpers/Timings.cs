using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

namespace TUnit.Engine.Helpers;

internal static class Timings
{
    public static void Record(string name, TestContext context, Action action)
    {
        var start = DateTimeOffset.Now;

        try
        {
            action();
        }
        finally
        {
            var end = DateTimeOffset.Now;
            
            // ConcurrentBag is lock-free and thread-safe
            context.Timings.Add(new Timing(name, start, end));
        }
    }

    public static Task Record(string name, TestContext context, Func<Task> action)
    {
        return Record(name, context, () => new ValueTask(action()));
    }

    public static async Task Record(string name, TestContext context, Func<ValueTask> action)
    {
        var start = DateTimeOffset.Now;

        try
        {
            await action();
        }
        finally
        {
            var end = DateTimeOffset.Now;
            
            // ConcurrentBag is lock-free and thread-safe
            context.Timings.Add(new Timing(name, start, end));
        }
    }

    public static TimingProperty GetTimingProperty(TestContext testContext, DateTimeOffset overallStart)
    {
        var end = DateTimeOffset.Now;

        // ConcurrentBag enumeration is thread-safe without explicit locking
        var stepTimings = testContext.Timings.Select(x =>
            new StepTimingInfo(x.StepName, string.Empty, new TimingInfo(x.Start, x.End, x.Duration)));

        return new TimingProperty(new TimingInfo(overallStart, end, end - overallStart), stepTimings.ToArray());
    }
}
