using System.Diagnostics;

namespace TUnit.Assertions.Tests;

[NotInParallel]
public class ExecutionTimeTests
{
    [Test]
    public async Task Completes_Within_Happy()
    {
        var timeProvider = TimeProviderContext.Current;
        var action = () => Thread.Sleep(1); // Still using Thread.Sleep as it's synchronous blocking

        await Assert.That(action).CompletesWithin(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task Completes_Within_Unhappy()
    {
        var timeProvider = TimeProviderContext.Current;
        var stopwatch = Stopwatch.StartNew();

        var action = () => Thread.Sleep(10000); // Still using Thread.Sleep as it's synchronous blocking

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took too long to complete"));

        var duration = stopwatch.Elapsed;

        await Assert.That(duration).IsLessThan(TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task Completes_Within_Happy2()
    {
        var timeProvider = TimeProviderContext.Current;
        var action = () => timeProvider.Delay(TimeSpan.FromMilliseconds(1));

        await Assert.That(action).CompletesWithin(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task Completes_Within_Unhappy2()
    {
        var timeProvider = TimeProviderContext.Current;
        var stopwatch = Stopwatch.StartNew();

        var action = () => timeProvider.Delay(TimeSpan.FromSeconds(10));

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took too long to complete"));

        var duration = stopwatch.Elapsed;

        await Assert.That(duration).IsLessThan(TimeSpan.FromSeconds(1));
    }
}
