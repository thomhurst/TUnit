using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Discovery;
using TUnit.Core.Tracking;

namespace TUnit.UnitTests;

/// <summary>
/// When object-graph discovery throws part-way through <see cref="ObjectTracker.TrackObjects"/>, the objects
/// it had already added to the test's tracked set are removed again. References are only counted once
/// discovery finishes, so leaving them would make the test release references it never took, and could
/// dispose a shared instance that another test still holds (#7156). The test's root objects already exist,
/// so they are still tracked and counted, and disposed when the test completes (#7213).
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
    public async Task Failed_Discovery_Still_Tracks_And_Disposes_Root_Objects()
    {
        var discoverer = new FakeDiscoverer();
        var tracker = new ObjectTracker(new TrackableObjectGraphProvider(discoverer), GlobalContext.Current.Disposer);

        var shared = new TrackedDisposable();
        var root = new TrackedDisposable();
        var nested = new TrackedDisposable();
        var holder = CreateContext();
        var testContext = CreateContext();

        // Another test holds the shared root (reference count 1)
        discoverer.Discover = context => Add(context, shared);
        tracker.TrackObjects(holder);

        // Discovery reaches a nested object, then a getter throws (#7213)
        discoverer.Discover = context =>
        {
            Add(context, root);
            Add(context, nested, depth: 1);
            throw new InvalidOperationException("getter threw");
        };
        discoverer.TrackRoots = context =>
        {
            Add(context, root);
            return Add(context, shared);
        };

        await Assert.That(() => tracker.TrackObjects(testContext)).Throws<InvalidOperationException>();

        // The roots are kept and counted; what nested discovery added is dropped
        await Assert.That(testContext.TrackedObjects.Values.SelectMany(x => x)).IsEquivalentTo(new object[] { root, shared });

        await tracker.UntrackObjects(testContext);

        await Assert.That(root.IsDisposed).IsTrue();
        await Assert.That(nested.IsDisposed).IsFalse();
        await Assert.That(shared.IsDisposed).IsFalse();

        await tracker.UntrackObjects(holder);

        await Assert.That(shared.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Failing_Root_Fallback_Keeps_The_Discovery_Exception_And_Rolls_Back()
    {
        var discoverer = new FakeDiscoverer();
        var tracker = new ObjectTracker(new TrackableObjectGraphProvider(discoverer), GlobalContext.Current.Disposer);

        var kept = new TrackedDisposable();
        var testContext = CreateContext();

        // An earlier registration of this test tracked `kept`
        discoverer.Discover = context => Add(context, kept);
        tracker.TrackObjects(testContext);

        discoverer.Discover = _ => throw new InvalidOperationException("getter threw");
        discoverer.TrackRoots = context =>
        {
            Add(context, new TrackedDisposable());
            throw new NotSupportedException("fallback threw");
        };

        await Assert.That(() => tracker.TrackObjects(testContext)).Throws<InvalidOperationException>()
            .WithMessage("getter threw");

        // Nothing the fallback added was counted, so it is dropped again
        await Assert.That(testContext.TrackedObjects.Values.SelectMany(x => x)).IsEquivalentTo(new object[] { kept });
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

    private static SortedList<int, HashSet<object>> Add(TestContext context, object obj, int depth = 0)
    {
        var tracked = context.TrackedObjects;
        if (!tracked.TryGetValue(depth, out var bucket))
        {
            bucket = new HashSet<object>(TUnit.Core.Helpers.ReferenceEqualityComparer.Instance);
            tracked[depth] = bucket;
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

        public Func<TestContext, SortedList<int, HashSet<object>>> TrackRoots { get; set; } = context => context.TrackedObjects;

        public SortedList<int, HashSet<object>> TrackRootObjects(TestContext testContext)
            => TrackRoots(testContext);

        public ObjectGraph DiscoverObjectGraph(TestContext testContext, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ObjectGraph DiscoverNestedObjectGraph(object rootObject, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
