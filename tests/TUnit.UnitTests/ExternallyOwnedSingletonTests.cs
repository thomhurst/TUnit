using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Discovery;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.UnitTests;

/// <summary>
/// A root singleton can reach tests both through a dependency injection scope, which marks what it returns
/// as externally owned, and through a data source that does not own it. These tests pin what the object
/// tracker does with such an object in each order, including when the scope's mark races with another
/// source's count (#7205).
/// </summary>
public class ExternallyOwnedSingletonTests
{
    // Synthetic contexts never reach the engine's cleanup, so each test removes its own from the registry.
    private readonly List<TestContext> _contexts = [];

    [After(Test)]
    public void RemoveContextsFromRegistry()
    {
        foreach (var context in _contexts)
        {
            context.RemoveFromRegistry();
        }
    }

    [Test]
    public async Task Marked_Before_Another_Source_Supplies_It_Is_Never_Disposed_By_The_Tracker()
    {
        var tracker = CreateTracker();
        var singleton = new TrackedDisposable();

        ObjectTracker.MarkExternallyOwned(singleton);

        var nonOwningTest = CreateContext(singleton);
        tracker.TrackObjects(nonOwningTest);
        await tracker.UntrackObjects(nonOwningTest);

        await Assert.That(ObjectTracker.IsExternallyOwned(singleton)).IsTrue();
        await Assert.That(singleton.DisposeCount).IsEqualTo(0);
    }

    [Test]
    public async Task Counted_Before_The_Scope_Returns_It_Is_Released_Without_Disposal()
    {
        var tracker = CreateTracker();
        var singleton = new TrackedDisposable();

        var nonOwningTest = CreateContext(singleton);
        tracker.TrackObjects(nonOwningTest);

        // A non-owning source can supply a container singleton before its owning source does.
        ObjectTracker.MarkExternallyOwned(singleton);
        await Assert.That(ObjectTracker.IsExternallyOwned(singleton)).IsTrue();

        await tracker.UntrackObjects(nonOwningTest);

        await Assert.That(singleton.DisposeCount).IsEqualTo(0);
        await Assert.That(IsCounted(singleton)).IsFalse();
    }

    [Test]
    public async Task Counted_Between_The_Check_And_The_Mark_Is_Released_Without_Being_Disposed()
    {
        var tracker = CreateTracker();
        var singleton = new TrackedDisposable();

        var nonOwningTest = CreateContext(singleton);
        tracker.TrackObjects(nonOwningTest);

        // MarkExternallyOwned found no count, then another source counted the object before the mark landed.
        MarkBypassingTheCountCheck(singleton);

        await tracker.UntrackObjects(nonOwningTest);

        await Assert.That(singleton.DisposeCount).IsEqualTo(0);

        // The count was released, so the session-end sweep will not dispose the object either.
        await Assert.That(IsCounted(singleton)).IsFalse();

        // A later source balances its own reference without taking disposal ownership.
        var laterTest = CreateContext(singleton);
        tracker.TrackObjects(laterTest);
        await tracker.UntrackObjects(laterTest);

        await Assert.That(singleton.DisposeCount).IsEqualTo(0);
    }

    [Test]
    public async Task Consumers_After_Ownership_Mark_Hold_Their_Own_References()
    {
        var tracker = CreateTracker();
        var singleton = new TrackedDisposable();
        var earlierTest = CreateContext(singleton);
        tracker.TrackObjects(earlierTest);
        ObjectTracker.MarkExternallyOwned(singleton);
        var laterTest = CreateContext(singleton);
        tracker.TrackObjects(laterTest);

        // Both consumers must hold a reference before either can release it.
        await Assert.That(ObjectTracker.IsShared(singleton)).IsTrue();
        await Task.WhenAll(
            Task.Run(async () => await tracker.UntrackObjects(earlierTest)),
            Task.Run(async () => await tracker.UntrackObjects(laterTest)));

        await Assert.That(IsCounted(singleton)).IsFalse();
        await Assert.That(singleton.DisposeCount).IsEqualTo(0);
    }

    private static void MarkBypassingTheCountCheck(object obj)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var table = (ConditionalWeakTable<object, object>) typeof(ObjectTracker).GetField("s_externallyOwned", flags)!.GetValue(null)!;
        table.Add(obj, new object());
        typeof(ObjectTracker).GetField("s_hasExternallyOwned", flags)!.SetValue(null, true);
    }

    private static bool IsCounted(object obj)
    {
        var counters = (System.Collections.IDictionary) typeof(ObjectTracker)
            .GetField("s_trackedObjects", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        return counters.Contains(obj);
    }

    private static ObjectTracker CreateTracker()
        => new(new TrackableObjectGraphProvider(new RootsOnlyDiscoverer()), GlobalContext.Current.Disposer);

    private TestContext CreateContext(object root)
    {
        var currentContext = TestContext.Current!;
        var context = new TestContext(nameof(ExternallyOwnedSingletonTests), currentContext.ServiceProvider, currentContext.ClassContext,
            new TestBuilderContext { TestMetadata = currentContext.TestDetails.MethodMetadata }, CancellationToken.None);
        Roots.Add(context, root);
        _contexts.Add(context);
        return context;
    }

    // The root each context's data source supplied, which discovery adds to the context's tracked objects.
    private static readonly ConditionalWeakTable<TestContext, object> Roots = new();

    private sealed class TrackedDisposable : IDisposable
    {
        private int _disposeCount;

        public int DisposeCount => _disposeCount;

        public void Dispose() => Interlocked.Increment(ref _disposeCount);
    }

    private sealed class RootsOnlyDiscoverer : IObjectGraphDiscoverer
    {
        public SortedList<int, HashSet<object>> DiscoverAndTrackObjects(TestContext testContext, CancellationToken cancellationToken = default)
            => TrackRootObjects(testContext);

        public SortedList<int, HashSet<object>> TrackRootObjects(TestContext testContext)
        {
            var tracked = testContext.TrackedObjects;
            if (Roots.TryGetValue(testContext, out var root))
            {
                tracked[0] = new HashSet<object>(TUnit.Core.Helpers.ReferenceEqualityComparer.Instance) { root };
            }

            return tracked;
        }

        public ObjectGraph DiscoverObjectGraph(TestContext testContext, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ObjectGraph DiscoverNestedObjectGraph(object rootObject, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
