using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Discovery;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.UnitTests;

/// <summary>
/// A root singleton can reach tests both through a dependency injection scope, which registers what it
/// owns, and through a data source that does not own it. These tests pin reference balancing in each
/// order and ensure owner handles survive overlapping consumers without permanent object marks (#7205).
/// </summary>
public class ExternallyOwnedSingletonTests
{
    // Synthetic contexts never reach the engine's cleanup, so each test removes its own from the registry.
    [Test]
    public async Task Tracker_Releases_Completed_Row_Handles_Before_Session_End()
    {
        var tracker = CreateTracker();
        var singleton = new TrackedDisposable();
        var owner = await TrackAndReleaseClonedRows(tracker, singleton);
        Collect();
        await Assert.That(OwnerIsAlive(owner)).IsFalse();
        await Assert.That(singleton.DisposeCount).IsEqualTo(0);
        GC.KeepAlive(tracker);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task<WeakReference<DataRowScopeCollection.ScopeOwner>> TrackAndReleaseClonedRows(
        ObjectTracker tracker, TrackedDisposable singleton)
    {
        var first = CreateContext(singleton);
        var second = CreateContext(singleton);
        var owner = AttachClonedScopes(first, second, singleton);
        tracker.TrackObjects(first);
        tracker.TrackObjects(second);
        DetachScopes(first);
        DetachScopes(second);
        await tracker.UntrackObjects(first);
        Collect();
        await Assert.That(OwnerIsAlive(owner)).IsTrue();
        await tracker.UntrackObjects(second);
        return owner;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DetachScopes(TestContext context) => context.InternalEvents.DetachDataRowScopes();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<DataRowScopeCollection.ScopeOwner> AttachClonedScopes(
        TestContext first, TestContext second, object singleton)
    {
        var scopes = new DataRowScopeCollection();
        var owner = scopes.Register(new EmptyScope());
        scopes.RegisterObject(owner, singleton);
        first.InternalEvents.AddDataRowScopes(scopes);
        second.InternalEvents.AddDataRowScopes(scopes.Clone());
        return new(owner);
    }

    [Test]
    public async Task Later_Consumers_Do_Not_Release_The_First_Consumers_Reference()
    {
        var tracker = CreateTracker();
        var singleton = new TrackedDisposable();
        var first = CreateContext(singleton);
        tracker.TrackObjects(first);
        using var owner = new OwnershipRegistration(singleton);

        var laterConsumers = Enumerable.Range(0, 16).Select(_ => CreateContext(singleton)).ToArray();
        foreach (var context in laterConsumers)
        {
            tracker.TrackObjects(context);
        }

        await Assert.That(ObjectTracker.IsShared(singleton)).IsTrue();
        var errors = await Task.WhenAll(laterConsumers.Select(context => Task.Run(async () => await tracker.UntrackObjects(context))));
        await Assert.That(errors.All(x => x is null)).IsTrue();
        // Later consumers must leave the reference taken before owner registration intact.
        await Assert.That(IsCounted(singleton)).IsTrue();
        await Assert.That(ObjectTracker.IsShared(singleton)).IsFalse();

        await tracker.UntrackObjects(first);
        await Assert.That(IsCounted(singleton)).IsFalse();
        await Assert.That(singleton.DisposeCount).IsEqualTo(0);
    }

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
    public async Task Registered_Before_Another_Source_Supplies_It_Is_Not_Disposed_By_The_Tracker()
    {
        var tracker = CreateTracker();
        var singleton = new TrackedDisposable();

        using var owner = new OwnershipRegistration(singleton);

        var nonOwningTest = CreateContext(singleton);
        tracker.TrackObjects(nonOwningTest);
        await tracker.UntrackObjects(nonOwningTest);

        await Assert.That(ExternalObjectOwnership.IsOwned(singleton)).IsTrue();
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
        using var owner = new OwnershipRegistration(singleton);
        await Assert.That(ExternalObjectOwnership.IsOwned(singleton)).IsTrue();

        await tracker.UntrackObjects(nonOwningTest);

        await Assert.That(singleton.DisposeCount).IsEqualTo(0);
        await Assert.That(IsCounted(singleton)).IsFalse();
    }

    [Test]
    public async Task Registering_An_Owner_After_Counting_Protects_Current_And_Later_Consumers()
    {
        var tracker = CreateTracker();
        var singleton = new TrackedDisposable();

        var nonOwningTest = CreateContext(singleton);
        tracker.TrackObjects(nonOwningTest);

        // Registration does not check the reference count: it publishes the owner independently.
        using var owner = new OwnershipRegistration(singleton);

        await tracker.UntrackObjects(nonOwningTest);

        await Assert.That(singleton.DisposeCount).IsEqualTo(0);

        // The count was released, so the session-end sweep will not dispose the object either.
        await Assert.That(IsCounted(singleton)).IsFalse();

        // A later source balances its own reference without taking disposal ownership.
        var laterTest = CreateContext(singleton);
        var secondLaterTest = CreateContext(singleton);
        tracker.TrackObjects(laterTest);
        tracker.TrackObjects(secondLaterTest);
        await Assert.That(ObjectTracker.IsShared(singleton)).IsTrue();
        await tracker.UntrackObjects(laterTest);
        await tracker.UntrackObjects(secondLaterTest);

        await Assert.That(IsCounted(singleton)).IsFalse();
        await Assert.That(singleton.DisposeCount).IsEqualTo(0);
    }

    [Test]
    public async Task Consumers_After_Owner_Registration_Hold_Their_Own_References()
    {
        var tracker = CreateTracker();
        var singleton = new TrackedDisposable();
        var earlierTest = CreateContext(singleton);
        tracker.TrackObjects(earlierTest);
        using var owner = new OwnershipRegistration(singleton);
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

    [Test]
    public async Task An_Existing_Consumer_Retains_The_Owner_Until_Its_Last_Reference_Ends()
    {
        var tracker = CreateTracker();
        var singleton = new TrackedDisposable();
        var context = CreateContext(singleton);
        tracker.TrackObjects(context);

        var owner = RegisterTemporaryOwnership(singleton);
        Collect();
        await Assert.That(OwnerIsAlive(owner)).IsTrue();
        await tracker.UntrackObjects(context);
        await Assert.That(singleton.DisposeCount).IsEqualTo(0);

        Collect();
        await Assert.That(OwnerIsAlive(owner)).IsFalse();
        // TUnit can own a later use of the same object without a permanent process-wide mark.
        var nextContext = CreateContext(singleton);
        tracker.TrackObjects(nextContext);
        await tracker.UntrackObjects(nextContext);
        await Assert.That(singleton.DisposeCount).IsEqualTo(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<DataRowScopeCollection.ScopeOwner> RegisterTemporaryOwnership(object instance)
    {
        var scopes = new DataRowScopeCollection();
        var owner = scopes.Register(new EmptyScope());
        scopes.RegisterObject(owner, instance);
        return new(owner);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OwnerIsAlive(WeakReference<DataRowScopeCollection.ScopeOwner> reference)
        => reference.TryGetTarget(out _);

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private sealed class EmptyScope : IDisposable
    {
        public void Dispose() { }
    }

    private sealed class OwnershipRegistration : IDisposable
    {
        private readonly DataRowScopeCollection _scopes = new();

        public OwnershipRegistration(object instance)
        {
            var owner = _scopes.Register(new Scope());
            _scopes.RegisterObject(owner, instance);
        }

        public void Dispose() => GC.KeepAlive(_scopes);

        private sealed class Scope : IDisposable
        {
            public void Dispose() { }
        }
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
            new TestBuilderContext { TestMetadata = currentContext.TestDetails.MethodMetadata }, CancellationToken.None)
        {
            TestDetails = currentContext.TestDetails
        };
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
