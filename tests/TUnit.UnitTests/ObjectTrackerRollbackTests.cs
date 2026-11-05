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

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Session_Sweep_Preserves_Other_Sessions_References(bool shareObject)
    {
        var firstDiscoverer = new FakeDiscoverer();
        var secondDiscoverer = new FakeDiscoverer();
        var first = new ObjectTracker(new TrackableObjectGraphProvider(firstDiscoverer), GlobalContext.Current.Disposer);
        var second = new ObjectTracker(new TrackableObjectGraphProvider(secondDiscoverer), GlobalContext.Current.Disposer);
        var firstObject = new TrackedDisposable();
        var secondObject = shareObject ? firstObject : new TrackedDisposable();
        firstDiscoverer.Discover = context => Add(context, firstObject);
        secondDiscoverer.Discover = context => Add(context, secondObject);
        var firstContext = CreateContext();
        var secondContext = CreateContext();
        first.TrackObjects(firstContext);
        second.TrackObjects(secondContext);

        await first.DisposeAndClearStaticTrackingAsync();

        await Assert.That(firstObject.IsDisposed).IsEqualTo(!shareObject);
        await Assert.That(secondObject.IsDisposed).IsFalse();
        // Late cleanup of a retired session must not release the other session's reference.
        await first.UntrackObjects(firstContext);
        await first.DisposeAndClearStaticTrackingAsync();
        await Assert.That(secondObject.IsDisposed).IsFalse();
        await second.UntrackObjects(secondContext);
        await Assert.That(secondObject.DisposeCount).IsEqualTo(1);
        await second.DisposeAndClearStaticTrackingAsync();
        await Assert.That(secondObject.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task Session_Sweep_Does_Not_Release_A_Static_Lease_That_Outlives_Its_Tracker()
    {
        var discoverer = new FakeDiscoverer();
        var tracker = new ObjectTracker(new TrackableObjectGraphProvider(discoverer), GlobalContext.Current.Disposer);
        var resource = new TrackedDisposable();
        tracker.TrackStaticLeaseObject(resource);
        discoverer.Discover = context => Add(context, resource);
        tracker.TrackObjects(CreateContext());

        await tracker.DisposeAndClearStaticTrackingAsync();
        await Assert.That(resource.IsDisposed).IsFalse();
        await tracker.ReleaseStaticLeaseObjectAsync(resource);
        await Assert.That(resource.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task Non_Owning_Row_Refreshes_Changed_Dependencies_Of_A_Shared_Root()
    {
        var service = new ExternalObjectOwnershipTests.Service();
        var scopes = new DataRowScopeCollection();
        var owner = scopes.Register(new ExternalObjectOwnershipTests.Scope());
        scopes.RegisterObject(owner, service);
        scopes.CaptureOwnership();
        var secondScopes = new DataRowScopeCollection();
        var secondOwner = secondScopes.Register(new ExternalObjectOwnershipTests.Scope());
        secondScopes.RegisterObject(secondOwner, service);
        secondScopes.CaptureOwnership();
        var previous = service.Child;
        service.Child = new ExternalObjectOwnershipTests.Initializer();
        var discoverer = new FakeDiscoverer
        {
            Discover = context =>
            {
                Add(context, service);
                return Add(context, service.Child, 1);
            }
        };
        var tracker = new ObjectTracker(new TrackableObjectGraphProvider(discoverer), GlobalContext.Current.Disposer);
        var context = CreateContext();

        tracker.TrackObjects(context);
        await Assert.That(owner.Owns(service.Child)).IsTrue();
        await Assert.That(secondOwner.Owns(service.Child)).IsTrue();
        await Assert.That(ExternalObjectOwnership.IsOwned(service.Child)).IsTrue();
        await Assert.That(ExternalObjectOwnership.IsOwned(previous)).IsTrue();
        await tracker.UntrackObjects(context);
        await tracker.DisposeAndClearStaticTrackingAsync();
        GC.KeepAlive(scopes);
        GC.KeepAlive(secondScopes);
    }

    [Test]
    public async Task Non_Owning_Row_Refreshes_Only_Consumed_Graphs_From_A_Previous_Scope()
    {
        var first = new ExternalObjectOwnershipTests.Service();
        var second = new ExternalObjectOwnershipTests.Service();
        var retired = new RetiredService();
        var scopes = new DataRowScopeCollection();
        var owner = scopes.Register(new ExternalObjectOwnershipTests.Scope());
        scopes.RegisterObject(owner, first);
        scopes.RegisterObject(owner, second);
        scopes.RegisterObject(owner, retired);
        scopes.CaptureOwnership();
        var previous = first.Child;
        retired.Dispose();
        await scopes.CompleteAsync();
        first.Child = new ExternalObjectOwnershipTests.Initializer();
        second.Child = new ExternalObjectOwnershipTests.Initializer();
        var discoverer = new FakeDiscoverer
        {
            Discover = context =>
            {
                Add(context, first);
                Add(context, second);
                Add(context, first.Child, 1);
                return Add(context, second.Child, 1);
            }
        };
        var tracker = new ObjectTracker(new TrackableObjectGraphProvider(discoverer), GlobalContext.Current.Disposer);
        var context = CreateContext();
        try
        {
            tracker.TrackObjects(context);
            await Assert.That(ExternalObjectOwnership.IsOwned(first.Child)).IsTrue();
            await Assert.That(ExternalObjectOwnership.IsOwned(second.Child)).IsTrue();
            await Assert.That(ExternalObjectOwnership.IsOwned(previous)).IsTrue();
        }
        finally
        {
            await tracker.UntrackObjects(context);
            await tracker.DisposeAndClearStaticTrackingAsync();
            context.RemoveFromRegistry();
            context.Dispose();
            GC.KeepAlive(scopes);
        }
    }

    [Test]
    public async Task Counted_Consumer_Retains_An_Owner_Across_Collection_After_Row_Completion()
    {
        var tracker = new ObjectTracker(new(), GlobalContext.Current.Disposer);
        var service = new ExternalObjectOwnershipTests.Service();
        var reference = RegisterCompletedOwner(tracker, service);
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Assert.That(reference.TryGetTarget(out _)).IsTrue();
            await Assert.That(ExternalObjectOwnership.IsOwned(service.Child)).IsTrue();
        }
        finally
        {
            await tracker.ReleaseStaticLeaseObjectAsync(service.Child);
        }
        await Assert.That(service.Child.DisposeCount).IsEqualTo(0);
        GC.KeepAlive(service);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference<DataRowScopeCollection.ScopeOwner> RegisterCompletedOwner(
        ObjectTracker consumer, ExternalObjectOwnershipTests.Service service)
    {
        var scopes = new DataRowScopeCollection();
        var owner = scopes.Register(new ExternalObjectOwnershipTests.Scope());
        scopes.RegisterObject(owner, service);
        scopes.CaptureOwnership();
        consumer.TrackStaticLeaseObject(service.Child);
        // This test scope completes synchronously; keep the helper synchronous to prevent an async
        // state machine from retaining the owner independently of the consumer's count callback.
        var completion = owner.DisposeAsync();
        if (!completion.IsCompletedSuccessfully)
            throw new InvalidOperationException("The test scope must complete synchronously");
        return new(owner);
    }

    [Test]
    public async Task Smaller_Ownership_Refresh_Keeps_Injected_Aliases_Tracker_Owned()
    {
        var injected = new ExternalObjectOwnershipTests.Initializer();
        var service = new ExternalObjectOwnershipTests.AliasedService
        {
            First = new ExternalObjectOwnershipTests.AliasHolder { Alias = injected },
            Second = new ExternalObjectOwnershipTests.InjectedHolder { Injected = injected }
        };
        var scopes = new DataRowScopeCollection();
        scopes.RegisterObject(scopes.Register(new ExternalObjectOwnershipTests.Scope()), service);
        scopes.CaptureOwnership();
        var discoverer = new FakeDiscoverer
        {
            Discover = context =>
            {
                Add(context, service);
                Add(context, service.First, 1);
                Add(context, service.Second, 1);
                return Add(context, injected, 2);
            }
        };
        var tracker = new ObjectTracker(new TrackableObjectGraphProvider(discoverer), GlobalContext.Current.Disposer);
        var context = CreateContext();
        try
        {
            tracker.TrackObjects(context);
            await Assert.That(ExternalObjectOwnership.IsOwned(injected)).IsFalse();
            await tracker.UntrackObjects(context);
            await Assert.That(injected.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            await tracker.DisposeAndClearStaticTrackingAsync();
            context.RemoveFromRegistry();
            context.Dispose();
            GC.KeepAlive(scopes);
        }
    }

    public sealed class RetiredService : IDisposable
    {
        private readonly ExternalObjectOwnershipTests.Initializer _child = new();
        private bool _disposed;
        public ExternalObjectOwnershipTests.Initializer Child
            => _disposed ? throw new ObjectDisposedException(nameof(RetiredService)) : _child;
        public void Dispose() => _disposed = true;
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
            new TestBuilderContext { TestMetadata = currentContext.TestDetails.MethodMetadata }, CancellationToken.None)
        {
            TestDetails = currentContext.TestDetails
        };
    }

    private sealed class TrackedDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public int DisposeCount { get; private set; }

        public void Dispose() { IsDisposed = true; DisposeCount++; }
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
