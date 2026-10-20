using System.Diagnostics;

namespace TUnit.Assertions.Tests;

[NotInParallel]
public class ExecutionTimeTests
{
    [Test]
    public async Task Completes_Within_Happy()
    {
        var action = () => Thread.Sleep(1);

        await Assert.That(action).CompletesWithin(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task Completes_Within_Unhappy()
    {
        var stopwatch = Stopwatch.StartNew();

        var action = () => Thread.Sleep(10000);

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took too long to complete"));

        var duration = stopwatch.Elapsed;

        // Well under the 10s action, so the 250ms limit must have cut it short, but loose enough
        // for loaded CI runners where a sub-second budget is flaky (see #7169).
        await Assert.That(duration).IsLessThan(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Completes_Within_Happy2()
    {
        var action = () => Task.Delay(1);

        await Assert.That(action).CompletesWithin(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task Completes_Within_Unhappy2()
    {
        var stopwatch = Stopwatch.StartNew();

        var action = () => Task.Delay(10000);

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took too long to complete"));

        var duration = stopwatch.Elapsed;

        // Well under the 10s action, so the 250ms limit must have cut it short, but loose enough
        // for loaded CI runners where a sub-second budget is flaky (see #7169).
        await Assert.That(duration).IsLessThan(TimeSpan.FromSeconds(5));
    }
}
