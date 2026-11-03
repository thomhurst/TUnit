using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject.DeferEnumerationTests;

[WarmDeferredLookup]
public class DeferredDependencyTests
{
    private sealed class SessionCounters
    {
        public int Completed;
        public int DependentCompleted;
    }

    private static readonly ConditionalWeakTable<TestSessionContext, SessionCounters> Counters = new();
    private static SessionCounters CurrentCounters => Counters.GetValue(TestSessionContext.Current!, static _ => new());

    public static IEnumerable<int> Rows() => Enumerable.Range(1, 3);
    public static IEnumerable<int> DependentRows() => Enumerable.Range(1, 2);

    [Test]
    [MethodDataSource(nameof(Rows), DeferEnumeration = true)]
    public async Task Deferred(int row)
    {
        await Task.Yield();
        Interlocked.Increment(ref CurrentCounters.Completed);
    }

    [Test]
    [DependsOn(nameof(Deferred))]
    [MethodDataSource(nameof(DependentRows), DeferEnumeration = true)]
    public async Task Depends_On_Deferred(int row)
    {
        await Assert.That(CurrentCounters.Completed).IsEqualTo(3);
        await Assert.That(TestContext.Current!.Dependencies.DependsOn.Count).IsEqualTo(3);
        await Assert.That(TestContext.Current.Dependencies.DependsOn.All(test => test.TestMethodArguments.Length == 1)).IsTrue();
        var results = TestContext.Current.Dependencies.GetTests(nameof(Deferred));
        await Assert.That(results.Count()).IsEqualTo(3);
        Interlocked.Increment(ref CurrentCounters.DependentCompleted);
    }

    [Test]
    [DependsOn(nameof(Depends_On_Deferred))]
    public async Task Waits_For_All_Rows()
    {
        await Assert.That(CurrentCounters.Completed).IsEqualTo(3);
        await Assert.That(CurrentCounters.DependentCompleted).IsEqualTo(2);
        // DependsOn includes the two direct rows and their three transitive dependencies.
        await Assert.That(TestContext.Current!.Dependencies.DependsOn.Count).IsEqualTo(5);
        await Assert.That(TestContext.Current.Dependencies.DependsOn.All(test => test.TestMethodArguments.Length == 1)).IsTrue();
    }
}

// Build TestFinder's indexes during registration, before placeholders expand, so the
// result assertions also exercise invalidation of an already-populated index.
public sealed class WarmDeferredLookupAttribute : Attribute, ITestRegisteredEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        _ = context.TestContext.Dependencies.GetTests("__NoSuchDeferredTest");
        return default;
    }
}
