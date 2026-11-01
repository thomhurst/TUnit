using TUnit.Core;
using TUnit.Core.Discovery;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.UnitTests;

/// <summary>
/// A shared instance whose last holder has ended is disposed and evicted, so a later request
/// creates a new instance instead of receiving the disposed one. A running test that fetches a
/// shared instance through <see cref="SharedDataSources"/> holds it until the test ends (#7204).
/// </summary>
public class SharedInstanceLifetimeTests
{
    [Test]
    public async Task Disposed_Shared_Instance_Is_Not_Handed_Out_Again()
    {
        var key = NewKey();
        var (tracker, discoverer) = CreateTracker();
        var holder = CreateContext();

        var first = Fetch(key);
        discoverer.Discover = context => Add(context, first);
        tracker.TrackObjects(holder);

        await tracker.UntrackObjects(holder);
        await Assert.That(first.IsDisposed).IsTrue();

        var second = Fetch(key);
        await Assert.That(second).IsNotSameReferenceAs(first);
        await Assert.That(second.IsDisposed).IsFalse();
    }

    [Test]
    public async Task Running_Test_Holds_Instance_It_Fetches_Until_It_Ends()
    {
        var key = NewKey();
        var (tracker, discoverer) = CreateTracker();
        var holder = CreateContext();
        var running = CreateContext();

        var instance = Fetch(key);
        discoverer.Discover = context => Add(context, instance);
        tracker.TrackObjects(holder);

        running.StartHoldingSharedReferences();
        using (running.MakeCurrent())
        {
            await Assert.That(SharedDataSources.GetOrCreate<SharedDisposable>(SharedType.Keyed, key: key)).IsSameReferenceAs(instance);
        }

        await tracker.UntrackObjects(holder);
        await Assert.That(instance.IsDisposed).IsFalse();

        running.StopHoldingSharedReferences();
        await tracker.UntrackObjects(running);
        await Assert.That(instance.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Running_Test_Gets_A_New_Instance_After_The_Last_Holder_Disposed_It()
    {
        var key = NewKey();
        var (tracker, discoverer) = CreateTracker();
        var holder = CreateContext();
        var running = CreateContext();

        var disposed = Fetch(key);
        discoverer.Discover = context => Add(context, disposed);
        tracker.TrackObjects(holder);
        await tracker.UntrackObjects(holder);

        running.StartHoldingSharedReferences();
        SharedDisposable fetched;
        using (running.MakeCurrent())
        {
            fetched = SharedDataSources.GetOrCreate<SharedDisposable>(SharedType.Keyed, key: key);
        }

        await Assert.That(fetched).IsNotSameReferenceAs(disposed);
        await Assert.That(fetched.IsDisposed).IsFalse();

        running.StopHoldingSharedReferences();
        await tracker.UntrackObjects(running);
        await Assert.That(fetched.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Fetching_An_Instance_The_Test_Already_Holds_Does_Not_Count_It_Twice()
    {
        var key = NewKey();
        var (tracker, discoverer) = CreateTracker();
        var running = CreateContext();

        var instance = Fetch(key);
        discoverer.Discover = context => Add(context, instance);
        tracker.TrackObjects(running);

        running.StartHoldingSharedReferences();
        using (running.MakeCurrent())
        {
            SharedDataSources.GetOrCreate<SharedDisposable>(SharedType.Keyed, key: key);
            SharedDataSources.GetOrCreate<SharedDisposable>(SharedType.Keyed, key: key);
        }

        running.StopHoldingSharedReferences();
        await tracker.UntrackObjects(running);
        await Assert.That(instance.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Fetching_Outside_The_Running_Window_Does_Not_Hold()
    {
        var key = NewKey();
        var registering = CreateContext();
        var ended = CreateContext();
        ended.StartHoldingSharedReferences();
        ended.StopHoldingSharedReferences();

        using (registering.MakeCurrent())
        {
            SharedDataSources.GetOrCreate<SharedDisposable>(SharedType.Keyed, key: key);
        }

        using (ended.MakeCurrent())
        {
            SharedDataSources.GetOrCreate<SharedDisposable>(SharedType.Keyed, key: key);
        }

        await Assert.That(registering.TrackedObjectsIfCreated?.Count ?? 0).IsEqualTo(0);
        await Assert.That(registering.HeldSharedInstancesIfCreated?.Count ?? 0).IsEqualTo(0);
        await Assert.That(ended.TrackedObjectsIfCreated?.Count ?? 0).IsEqualTo(0);
        await Assert.That(ended.HeldSharedInstancesIfCreated?.Count ?? 0).IsEqualTo(0);
    }

    [Test]
    public async Task Held_Instances_Are_Kept_Out_Of_The_Tracked_Objects()
    {
        // The engine initializes everything in TrackedObjects, and an initializer can fetch another
        // shared instance while that walk runs. Held instances must not be initialized or change
        // the set the walk reads.
        var key = NewKey();
        var (tracker, _) = CreateTracker();
        var running = CreateContext();

        running.StartHoldingSharedReferences();
        SharedDisposable instance;
        using (running.MakeCurrent())
        {
            instance = SharedDataSources.GetOrCreate<SharedDisposable>(SharedType.Keyed, key: key);
        }

        await Assert.That(running.TrackedObjectsIfCreated?.Count ?? 0).IsEqualTo(0);
        await Assert.That(running.HeldSharedInstances).Contains(instance);

        running.StopHoldingSharedReferences();
        await tracker.UntrackObjects(running);
        await Assert.That(instance.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Instance_Cached_Under_Several_Keys_Is_Evicted_From_All_Of_Them()
    {
        var firstKey = NewKey();
        var secondKey = NewKey();
        var (tracker, discoverer) = CreateTracker();
        var holder = CreateContext();
        var running = CreateContext();

        var shared = new SharedDisposable();
        using (CreateContext().MakeCurrent())
        {
            SharedDataSources.GetOrCreate(SharedType.Keyed, (Type?) null, firstKey, () => shared);
            SharedDataSources.GetOrCreate(SharedType.Keyed, (Type?) null, secondKey, () => shared);
        }

        discoverer.Discover = context => Add(context, shared);
        tracker.TrackObjects(holder);
        await tracker.UntrackObjects(holder);
        await Assert.That(shared.IsDisposed).IsTrue();

        running.StartHoldingSharedReferences();
        var fetched = await Task.Run(() =>
        {
            using (running.MakeCurrent())
            {
                return (
                    First: SharedDataSources.GetOrCreate(SharedType.Keyed, (Type?) null, firstKey, () => new SharedDisposable()),
                    Second: SharedDataSources.GetOrCreate(SharedType.Keyed, (Type?) null, secondKey, () => new SharedDisposable()));
            }
        }).WaitAsync(TimeSpan.FromSeconds(30));

        await Assert.That(fetched.First).IsNotSameReferenceAs(shared);
        await Assert.That(fetched.First.IsDisposed).IsFalse();
        await Assert.That(fetched.Second).IsNotSameReferenceAs(shared);
        await Assert.That(fetched.Second.IsDisposed).IsFalse();

        running.StopHoldingSharedReferences();
        await tracker.UntrackObjects(running);
    }

    [Test]
    public async Task Instance_Held_Again_After_Its_Count_Reached_Zero_Is_Disposed_Once()
    {
        // The last holder's count reaches zero; before it retires the instance, a running test
        // holds the instance, ends, and retires and disposes it. The first holder must not then
        // dispose it a second time.
        var key = NewKey();
        var (tracker, discoverer) = CreateTracker();
        var holder = CreateContext();
        var running = CreateContext();

        var instance = Fetch(key);
        discoverer.Discover = context => Add(context, instance);
        tracker.TrackObjects(holder);

        // Raised synchronously when the count reaches zero, before the holder retires the instance.
        ObjectTracker.OnDisposed(instance, () =>
        {
            running.StartHoldingSharedReferences();
            using (running.MakeCurrent())
            {
                SharedDataSources.GetOrCreate<SharedDisposable>(SharedType.Keyed, key: key);
            }

            running.StopHoldingSharedReferences();
            var untracking = tracker.UntrackObjects(running);
            if (!untracking.IsCompleted)
            {
                throw new InvalidOperationException("Expected synchronous disposal.");
            }
        });

        await tracker.UntrackObjects(holder);

        await Assert.That(instance.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task Concurrent_Fetches_In_One_Test_Hold_The_Instance_Once()
    {
        var key = NewKey();
        var (tracker, _) = CreateTracker();
        var running = CreateContext();

        running.StartHoldingSharedReferences();
        var fetched = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
        {
            using (running.MakeCurrent())
            {
                return SharedDataSources.GetOrCreate<SharedDisposable>(SharedType.Keyed, key: key);
            }
        })));

        var instance = fetched[0];
        await Assert.That(fetched.All(x => ReferenceEquals(x, instance))).IsTrue();

        running.StopHoldingSharedReferences();
        await tracker.UntrackObjects(running);
        await Assert.That(instance.IsDisposed).IsTrue();
    }

    private static string NewKey() => $"{nameof(SharedInstanceLifetimeTests)}-{Guid.NewGuid():N}";

    // Fetched by a test that is not running yet, as a data source does at registration.
    private static SharedDisposable Fetch(string key)
    {
        using (CreateContext().MakeCurrent())
        {
            return SharedDataSources.GetOrCreate<SharedDisposable>(SharedType.Keyed, key: key);
        }
    }

    private static (ObjectTracker Tracker, FakeDiscoverer Discoverer) CreateTracker()
    {
        var discoverer = new FakeDiscoverer();
        return (new ObjectTracker(new TrackableObjectGraphProvider(discoverer), GlobalContext.Current.Disposer), discoverer);
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
        return new TestContext(nameof(SharedInstanceLifetimeTests), currentContext.ServiceProvider, currentContext.ClassContext,
            new TestBuilderContext { TestMetadata = currentContext.TestDetails.MethodMetadata }, CancellationToken.None);
    }

    public sealed class SharedDisposable : IDisposable
    {
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public bool IsDisposed => DisposeCount > 0;

        public void Dispose() => Interlocked.Increment(ref _disposeCount);
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
