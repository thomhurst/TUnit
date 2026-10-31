using TUnit.Core;
using TUnit.Engine.Services;

namespace TUnit.UnitTests;

public class UnobservedTaskExceptionTrackerTests
{
    private static TestSessionContext CreateSession()
    {
        var beforeDiscoveryContext = new BeforeTestDiscoveryContext { TestFilter = null };
        var discoveryContext = new TestDiscoveryContext(beforeDiscoveryContext) { TestFilter = null };
        return new TestSessionContext(discoveryContext)
        {
            Id = Guid.NewGuid().ToString(),
            TestFilter = null
        };
    }

    [Test]
    public async Task Single_Session_Records_Attributable_Exception()
    {
        var session = CreateSession();
        var cause = new InvalidOperationException("leaked");

        UnobservedTaskExceptionTracker.Record(new AggregateException(cause), [session]);

        var entries = session.GetUnobservedTaskExceptionEntries();
        await Assert.That(entries).HasSingleItem();
        await Assert.That(entries[0].Exception).IsSameReferenceAs(cause);
        await Assert.That(entries[0].Attributable).IsTrue();
        await Assert.That(session.UnobservedTaskExceptions).HasSingleItem();
    }

    [Test]
    public async Task Concurrent_Sessions_Record_Exception_As_Unattributable()
    {
        var first = CreateSession();
        var second = CreateSession();

        UnobservedTaskExceptionTracker.Record(new AggregateException(new InvalidOperationException("leaked")), [first, second]);

        foreach (var session in new[] { first, second })
        {
            var entries = session.GetUnobservedTaskExceptionEntries();
            await Assert.That(entries).HasSingleItem();
            await Assert.That(entries[0].Attributable).IsFalse();
        }
    }

    [Test]
    public async Task Each_Cause_Of_An_Aggregate_Is_Recorded_Separately()
    {
        var session = CreateSession();
        var first = new InvalidOperationException("first");
        var second = new ArgumentException("second");
        var nested = new FormatException("nested");

        UnobservedTaskExceptionTracker.Record(
            new AggregateException(first, new AggregateException(second, nested)), [session]);

        await Assert.That(session.UnobservedTaskExceptions).IsEquivalentTo(new Exception[] { first, second, nested });
    }
}
