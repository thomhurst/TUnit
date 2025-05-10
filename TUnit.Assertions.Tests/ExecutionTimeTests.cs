using System.Diagnostics;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Assertions.Delegates;

namespace TUnit.Assertions.Tests;

public class ExecutionTimeTests
{
    [Test]
    public async Task Completes_Within_Happy()
    {
        var action = () => Thread.Sleep(100);

        await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250));
    }
    
    [Test, NotInParallel]
    public async Task Completes_Within_Unhappy()
    {
        var stopwatch = Stopwatch.StartNew();
    
        var action = () => Thread.Sleep(1000);

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took too long to complete"));

        var duration = stopwatch.Elapsed;

        await Assert.That(duration).IsLessThan(TimeSpan.FromSeconds(1));
    }
    
    [Test, NotInParallel]
    public async Task Completes_Within_Happy2()
    {
        var action = () => Task.Delay(TimeSpan.FromSeconds(1));

        await Assert.That(action).CompletesWithin(TimeSpan.FromSeconds(1.5));
    }
    
    [Test, NotInParallel]
    public async Task Completes_Within_Unhappy2()
    {
        var stopwatch = Stopwatch.StartNew();

        var action = () => Task.Delay(1000);

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took too long to complete"));
        
        var duration = stopwatch.Elapsed;

        await Assert.That(duration).IsLessThan(TimeSpan.FromSeconds(1));
    }
}