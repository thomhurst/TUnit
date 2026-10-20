using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Discovery;
using TUnit.Core.Tracking;

namespace TUnit.UnitTests;

/// <summary>
/// When object-graph discovery throws part-way through <see cref="ObjectTracker.TrackObjects"/>, the objects
/// it had already added to the test's tracked set are removed again. References are only counted once
/// discovery finishes, so leaving them would make the test release references it never took, and could
/// dispose a shared instance that another test still holds (#7156).
/// </summary>
public class ObjectTrackerRollbackTests
{
    [Test]
    public async Task Failed_Discovery_Does_Not_Release_A_Reference_It_Never_Took()
    {
        var discoverer = new FakeDiscoverer();
        var tracker = new ObjectTracker(new TrackableObjectGraphProvider(discoverer), GlobalContext.Current.Disposer);

        var shared = new TrackedDisposable();
        var kept = new TrackedDisposable();
        var holder = CreateContext();
        var testContext = CreateContext();

        // Another test holds the shared instance (reference count 1)
        discoverer.Discover = context => Add(context, shared);
        tracker.TrackObjects(holder);

        // An earlier registration of this test tracked `kept`
        discoverer.Discover = context => Add(context, kept);
        tracker.TrackObjects(testContext);

        // A later registration reaches the shared instance, then a getter throws
        discoverer.Discover = context =>
        {
            Add(context, shared);
            throw new InvalidOperationException("getter threw");
        };

        await Assert.That(() => tracker.TrackObjects(testContext)).Throws<InvalidOperationException>();

        await Assert.That(testContext.TrackedObjects.Values.SelectMany(x => x)).IsEquivalentTo(new object[] { kept });

        await tracker.UntrackObjects(testContext);

        await Assert.That(kept.IsDisposed).IsTrue();
        await Assert.That(shared.IsDisposed).IsFalse();

        await tracker.UntrackObjects(holder);

        await Assert.That(shared.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Failed_First_Discovery_Leaves_Nothing_Tracked()
    {
        var discoverer = new FakeDiscoverer
        {
            Discover = context =>
            {
                Add(context, new TrackedDisposable());
                throw new InvalidOperationException("getter threw");
            }
        };
        var tracker = new ObjectTracker(new TrackableObjectGraphProvider(discoverer), GlobalContext.Current.Disposer);
        var testContext = CreateContext();

        await Assert.That(() => tracker.TrackObjects(testContext)).Throws<InvalidOperationException>();

        await Assert.That(testContext.TrackedObjectsIfCreated?.Count ?? 0).IsEqualTo(0);
    }

    private static SortedList<int, HashSet<object>> Add(TestContext context, object obj)
    {
        var tracked = context.TrackedObjects;
        if (!tracked.TryGetValue(0, out var bucket))
        {
            bucket = new HashSet<object>(TUnit.Core.Helpers.ReferenceEqualityComparer.Instance);
            tracked[0] = bucket;
        }

        bucket.Add(obj);
        return tracked;
    }

    private static TestContext CreateContext()
    {
        var currentContext = TestContext.Current!;
        return new TestContext(nameof(ObjectTrackerRollbackTests), currentContext.ServiceProvider, currentContext.ClassContext,
            new TestBuilderContext { TestMetadata = currentContext.TestDetails.MethodMetadata }, CancellationToken.None);
    }

    private sealed class TrackedDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class FakeDiscoverer : IObjectGraphDiscoverer
    {
        public Func<TestContext, SortedList<int, HashSet<object>>> Discover { get; set; } = context => context.TrackedObjects;

        public SortedList<int, HashSet<object>> DiscoverAndTrackObjects(TestContext testContext, CancellationToken cancellationToken = default)
            => Discover(testContext);

        public ObjectGraph DiscoverObjectGraph(TestContext testContext, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ObjectGraph DiscoverNestedObjectGraph(object rootObject, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
