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
    
    [Test]
    public async Task Completes_Within_Unhappy()
    {
        var action = () => Thread.Sleep(1000);

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took \d{1} seconds and \d+ milliseconds"));
    }
    
    [Test]
    public async Task Completes_Within_Happy2()
    {
        var action = () => Task.Delay(100);

        await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250));
    }
    
    [Test]
    public async Task Completes_Within_Unhappy2()
    {
        var action = () => Task.Delay(1000);

        await Assert.That(
            async () => await Assert.That(action).CompletesWithin(TimeSpan.FromMilliseconds(250))
        ).Throws<AssertionException>()
        .WithMessageMatching(StringMatcher.AsRegex(@"Expected action to complete within 250 milliseconds\s+but it took \d{1} seconds and \d+ milliseconds"));
    }
}