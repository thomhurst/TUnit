using System.Runtime.CompilerServices;
using TUnit.Core.Discovery;
using TUnit.Core.Interfaces;

namespace TUnit.UnitTests;

public class ExternalObjectOwnershipTests
{
    [Test]
    public async Task Concurrent_Capture_Registration_And_Counter_Release_Preserve_Owner_Lifetime()
    {
        var tracker = new TUnit.Core.Tracking.ObjectTracker(new(), GlobalContext.Current.Disposer);
        var service = new Service();
        var owner = await ExerciseConcurrentOwnership(tracker, service).WaitAsync(TimeSpan.FromSeconds(30));
        // Unwind the helper's completing continuation before testing weak reachability.
        await Task.Yield();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await Assert.That(IsOwnerAlive(owner)).IsTrue();
        await Assert.That(ExternalObjectOwnership.IsOwned(service.Child)).IsTrue();

        await tracker.ReleaseStaticLeaseObjectAsync(service.Child);
        await Task.Yield();
        CollectOwner(owner);
        await Assert.That(ExternalObjectOwnership.IsOwned(service.Child)).IsFalse();
        await Assert.That(service.Child.DisposeCount).IsEqualTo(0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference<DataRowScopeCollection.ScopeOwner>> ExerciseConcurrentOwnership(
        TUnit.Core.Tracking.ObjectTracker tracker, Service service)
    {
        var scopes = new DataRowScopeCollection();
        var scope = new Scope();
        var owner = scopes.Register(scope);
        scopes.RegisterObject(owner, service);
        scopes.CaptureOwnership();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var capture = Task.Run(async () =>
        {
            await start.Task;
            for (var i = 0; i < 128; i++)
            {
                scopes.CaptureOwnership();
                await Task.Yield();
            }
        });
        var register = Task.Run(async () =>
        {
            await start.Task;
            for (var i = 0; i < 128; i++)
            {
                // Add a fresh root to enter pin publication, and refresh the shared registry entry.
                scopes.RegisterObject(owner, new Initializer());
                ExternalObjectOwnership.Register(service.Child, owner);
                await Task.Yield();
            }
        });
        var release = Task.Run(async () =>
        {
            await start.Task;
            for (var i = 0; i < 128; i++)
            {
                tracker.TrackStaticLeaseObject(service.Child);
                TUnit.Core.Tracking.ObjectTracker.OnDisposed(service.Child,
                    () => tracker.TrackStaticLeaseObject(service.Child));
                // Reaching zero invokes the pin callback and retires the counter while other
                // workers publish ownership. The reentrant consumer then releases its own lease.
                await tracker.ReleaseStaticLeaseObjectAsync(service.Child);
                await tracker.ReleaseStaticLeaseObjectAsync(service.Child);
                await Task.Yield();
            }
        });
        var acquire = Task.Run(async () =>
        {
            await start.Task;
            for (var i = 0; i < 128; i++)
            {
                // Contend with the callback's lease acquisition on an independent owned root.
                tracker.TrackStaticLeaseObject(service);
                await tracker.ReleaseStaticLeaseObjectAsync(service);
                await Task.Yield();
            }
        });
        var collect = Task.Run(async () =>
        {
            await start.Task;
            for (var i = 0; i < 8; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Yield();
            }
        });

        start.SetResult();
        await Task.WhenAll(capture, register, release, acquire, collect);
        await Assert.That(service.Child.DisposeCount).IsEqualTo(0);
        // Leave only a counted consumer retaining the completed row's owner after this helper returns.
        tracker.TrackStaticLeaseObject(service.Child);
        await scopes.CompleteAsync();
        await Assert.That(scope.DisposeCount).IsEqualTo(1);
        var reference = new WeakReference<DataRowScopeCollection.ScopeOwner>(owner);
        // Completed worker tasks may retain their shared closure in Debug builds. Remove its
        // strong row references so the caller measures the counter pin, not the test harness.
        scopes = null!;
        owner = null!;
        return reference;
    }

    [Test]
    public async Task Reentrant_Consumer_Keeps_Owner_Pinned_After_Zero_Count_Callback()
    {
        var tracker = new TUnit.Core.Tracking.ObjectTracker(new(), GlobalContext.Current.Disposer);
        var instance = new Initializer();
        var owner = RegisterReentrantConsumer(tracker, instance);
        await tracker.ReleaseStaticLeaseObjectAsync(instance);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await Assert.That(IsOwnerAlive(owner)).IsTrue();
        await tracker.ReleaseStaticLeaseObjectAsync(instance);
        CollectOwner(owner);
        await Assert.That(owner.TryGetTarget(out _)).IsFalse();
        await Assert.That(instance.DisposeCount).IsEqualTo(0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsOwnerAlive(WeakReference<DataRowScopeCollection.ScopeOwner> owner)
        => owner.TryGetTarget(out _);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<DataRowScopeCollection.ScopeOwner> RegisterReentrantConsumer(
        TUnit.Core.Tracking.ObjectTracker tracker, Initializer instance)
    {
        tracker.TrackStaticLeaseObject(instance);
        // Subscribe first: this consumer can re-enter before the owner's callback runs.
        TUnit.Core.Tracking.ObjectTracker.OnDisposed(instance, () => tracker.TrackStaticLeaseObject(instance));
        var scopes = new DataRowScopeCollection();
        var owner = scopes.Register(new Scope());
        scopes.RegisterObject(owner, instance);
        return new(owner);
    }

    [Test]
    public async Task Never_Tracked_Dependency_Is_Not_Pinned_By_Live_Owner()
    {
        var service = new Service();
        var scopes = new DataRowScopeCollection();
        scopes.RegisterObject(scopes.Register(new Scope()), service);
        var previous = CaptureAndDetach(scopes, service);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await Assert.That(previous.IsAlive).IsFalse();
        GC.KeepAlive(scopes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CaptureAndDetach(DataRowScopeCollection scopes, Service service)
    {
        scopes.CaptureOwnership();
        var previous = new WeakReference(service.Child);
        service.Child = new Initializer();
        return previous;
    }

    [Test]
    public async Task Later_Injected_Identity_Revokes_Previous_Dependency_Ownership()
    {
        var service = new Service();
        var injected = service.Child;
        var scopes = new DataRowScopeCollection();
        scopes.RegisterObject(scopes.Register(new Scope()), service);
        scopes.CaptureOwnership();
        await Assert.That(ExternalObjectOwnership.IsOwned(injected)).IsTrue();

        scopes.CaptureOwnership([injected, service]);
        await Assert.That(ExternalObjectOwnership.IsOwned(injected)).IsFalse();
        await Assert.That(ExternalObjectOwnership.IsOwned(service)).IsTrue();
        GC.KeepAlive(scopes);
    }

    [Test]
    public async Task Changed_Graph_Does_Not_Retain_Unreachable_Previous_Dependencies()
    {
        var service = new Service();
        var scopes = new DataRowScopeCollection();
        scopes.RegisterObject(scopes.Register(new Scope()), service);
        var previous = ReplaceDependency(scopes, service);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await Assert.That(previous.IsAlive).IsFalse();
        await Assert.That(ExternalObjectOwnership.IsOwned(service.Child)).IsTrue();
        GC.KeepAlive(scopes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ReplaceDependency(DataRowScopeCollection scopes, Service service)
    {
        scopes.CaptureOwnership();
        var previous = new WeakReference(service.Child);
        service.Child = new Initializer();
        scopes.CaptureOwnership();
        return previous;
    }

    [Test]
    public async Task Ownership_Ends_When_The_Last_Owner_Handle_Is_Collected()
    {
        var service = new Service();
        var reference = RegisterTemporaryOwner(service);
        CollectOwner(reference);

        await Assert.That(ExternalObjectOwnership.IsOwned(service)).IsFalse();
        await Assert.That(ExternalObjectOwnership.IsOwned(service.Child)).IsFalse();
        GC.KeepAlive(service);
    }

    [Test]
    public async Task Shared_Identity_Remains_Owned_When_One_Of_Its_Owners_Is_Collected()
    {
        var service = new Service();
        var scopes = new DataRowScopeCollection();
        scopes.RegisterObject(scopes.Register(new Scope()), service);
        scopes.CaptureOwnership();
        var temporary = RegisterTemporaryOwner(service);
        CollectOwner(temporary);

        await Assert.That(ExternalObjectOwnership.IsOwned(service)).IsTrue();
        await Assert.That(ExternalObjectOwnership.IsOwned(service.Child)).IsTrue();
        GC.KeepAlive(scopes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<DataRowScopeCollection.ScopeOwner> RegisterTemporaryOwner(Service service)
    {
        var scopes = new DataRowScopeCollection();
        var owner = scopes.Register(new Scope());
        scopes.RegisterObject(owner, service);
        scopes.CaptureOwnership();
        if (!ExternalObjectOwnership.IsOwned(service.Child))
            throw new InvalidOperationException("The owner's nested graph was not registered");
        return new(owner);
    }

    private static void CollectOwner(WeakReference<DataRowScopeCollection.ScopeOwner> reference)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        if (reference.TryGetTarget(out _))
            throw new InvalidOperationException("The owner is retained by the ownership registry");
    }

    [Test]
    public async Task Cloned_Row_Retains_The_Owner_After_Scope_Disposal()
    {
        var service = new Service();
        var scope = new Scope();
        var scopes = new DataRowScopeCollection();
        var owner = scopes.Register(scope);
        scopes.RegisterObject(owner, service);
        scopes.CaptureOwnership();
        var clone = scopes.Clone();

        await scopes.CompleteAsync();
        await clone.CompleteAsync();
        await Assert.That(scope.DisposeCount).IsEqualTo(1);
        // Scope disposal happens before tracker references are released. Owner handles must still
        // suppress tracker disposal, including nested initializers and cloned rows.
        await Assert.That(ExternalObjectOwnership.IsOwned(service)).IsTrue();
        await Assert.That(ExternalObjectOwnership.IsOwned(service.Child)).IsTrue();
        GC.KeepAlive(clone);
    }

    [Test]
    public async Task Pure_Discovery_Does_Not_Transfer_Ownership()
    {
        var service = new Service();
        var graph = new ObjectGraphDiscoverer().DiscoverStaticPropertyTrackableObjects(service, default);
        await Assert.That(graph.Values.Any(values => values.Contains(service.Child))).IsTrue();
        await Assert.That(ExternalObjectOwnership.IsOwned(service.Child)).IsFalse();
    }

    [Test]
    public async Task Snapshot_Excludes_An_Injected_Alias_Discovered_Through_Another_Branch()
    {
        var injected = new Initializer();
        var service = new AliasedService
        {
            First = new AliasHolder { Alias = injected },
            Second = new InjectedHolder { Injected = injected }
        };
        var scopes = new DataRowScopeCollection();
        var owner = scopes.Register(new Scope());
        scopes.RegisterObject(owner, service);
        scopes.CaptureOwnership();

        await Assert.That(ExternalObjectOwnership.IsOwned(service.First)).IsTrue();
        await Assert.That(ExternalObjectOwnership.IsOwned(service.Second)).IsTrue();
        await Assert.That(ExternalObjectOwnership.IsOwned(injected)).IsFalse();
        GC.KeepAlive(scopes);
    }

    [Test]
    public async Task Later_Snapshots_Keep_Previously_Registered_Dependencies_With_Their_Owner()
    {
        var service = new Service();
        var previous = service.Child;
        var scopes = new DataRowScopeCollection();
        scopes.RegisterObject(scopes.Register(new Scope()), service);
        scopes.CaptureOwnership();
        service.Child = new Initializer();
        scopes.CaptureOwnership();

        await Assert.That(ExternalObjectOwnership.IsOwned(previous)).IsTrue();
        await Assert.That(ExternalObjectOwnership.IsOwned(service.Child)).IsTrue();
        GC.KeepAlive(scopes);
    }

    [Test]
    public async Task Non_Disposable_Scope_Does_Not_Own_Its_Objects()
    {
        var service = new Service();
        var scopes = new DataRowScopeCollection();
        var owner = scopes.Register(new object());
        scopes.RegisterObject(owner, service);
        scopes.CaptureOwnership();
        await Assert.That(ExternalObjectOwnership.IsOwned(service)).IsFalse();
        await Assert.That(ExternalObjectOwnership.IsOwned(service.Child)).IsFalse();
        GC.KeepAlive(scopes);
    }

    public sealed class Scope : IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }

    public sealed class Service : IDisposable
    {
        public Initializer Child { get; set; } = new();
        public void Dispose() { }
    }

    public class Initializer : IAsyncInitializer, IDisposable
    {
        public int DisposeCount { get; private set; }
        public Task InitializeAsync() => Task.CompletedTask;
        public void Dispose() => DisposeCount++;
    }

    public sealed class AliasedService
    {
        public required AliasHolder First { get; init; }
        public required InjectedHolder Second { get; init; }
    }

    public sealed class AliasHolder : Initializer
    {
        public required Initializer Alias { get; init; }
    }

    public sealed class InjectedHolder : Initializer
    {
        [ClassDataSource<Initializer>]
        public required Initializer Injected { get; init; }
    }
}
