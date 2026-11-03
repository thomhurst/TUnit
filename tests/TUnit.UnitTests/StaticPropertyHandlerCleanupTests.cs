using TUnit.Core.Discovery;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;
using TUnit.Core.StaticProperties;
using TUnit.Engine.Services;

namespace TUnit.UnitTests;

public class StaticPropertyHandlerCleanupTests
{
    [Test]
    public async Task Static_Callbacks_Belong_To_Their_Session_Including_Failed_Initialization()
    {
        var first = CreateSession();
        var firstContext = TestSessionContext.GlobalStaticPropertyContext;
        var firstHandler = CreateHandler(first);
        var firstCount = 0;
        firstContext.Events.OnDispose += (_, _) => { firstCount++; return default; };

        var second = CreateSession();
        var secondContext = TestSessionContext.GlobalStaticPropertyContext;
        var secondHandler = CreateHandler(second);
        var secondCount = 0;
        secondContext.Events.OnDispose += (_, _) => { secondCount++; return default; };

        // Cleanup must use the handler's owner even while another session is ambient.
        await firstHandler.ReleaseSessionCallbacksAfterFailedInitializationAsync();
        await firstHandler.DisposeStaticPropertiesAsync([], []);
        await Assert.That(firstCount).IsEqualTo(1);
        await Assert.That(secondCount).IsEqualTo(0);
        await Assert.That(ReferenceEquals(firstContext, secondContext)).IsFalse();

        await secondHandler.DisposeStaticPropertiesAsync([], []);
        await secondHandler.ReleaseSessionCallbacksAfterFailedInitializationAsync();
        await Assert.That(secondCount).IsEqualTo(1);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Fixtures_Dispose_Before_Their_Scopes_Even_When_Teardown_Throws(bool throws)
    {
        var session = CreateSession();
        var fixture = new Fixture { Throws = throws };
        var provider = new TrackableObjectGraphProvider(new FixtureDiscoverer(fixture));
        var tracker = new ObjectTracker(provider, GlobalContext.Current.Disposer);
        var handler = new StaticPropertyHandler(null!, tracker, provider, null!, null!, session);
        var current = TestContext.Current!;
        var context = new TestContext(nameof(StaticPropertyHandlerCleanupTests), current.ServiceProvider, current.ClassContext,
            new TestBuilderContext { TestMetadata = current.TestDetails.MethodMetadata }, CancellationToken.None);
        tracker.TrackObjects(context);
        var scopesDisposed = 0;
        session.StaticPropertyContext.Events.OnDispose += (_, _) =>
        {
            fixture.ScopeDisposed = true;
            scopesDisposed++;
            return default;
        };
        var exceptions = new List<Exception>();
        if (throws)
        {
            await Assert.That(() => handler.DisposeStaticPropertiesAsync(exceptions, [fixture])).Throws<AggregateException>();
        }
        else
        {
            await handler.DisposeStaticPropertiesAsync(exceptions, [fixture]);
        }
        await Assert.That(fixture.DisposedWithLiveScope).IsTrue();
        await Assert.That(scopesDisposed).IsEqualTo(1);
        await Assert.That(exceptions.Count).IsEqualTo(throws ? 1 : 0);
    }

    [Test]
    public async Task Property_Metadata_Uses_Explicit_Owner_Events_Without_Method_Metadata()
    {
        var owner = CreateSession();
        var events = owner.StaticPropertyContext.Events;
        var bag = new System.Collections.Concurrent.ConcurrentDictionary<string, object?>();
        CreateSession(); // Another session is ambient while metadata is constructed.
        var property = new PropertyMetadata
        {
            Type = typeof(bool), Name = nameof(Fixture.ScopeDisposed), ReflectionInfo = null!,
            IsStatic = false, Getter = _ => false, ClassMetadata = null!, ContainingTypeMetadata = null!
        };
        var metadata = DataGeneratorMetadataCreator.CreateForPropertyInjection(
            property, null, new ArgumentsAttribute(false), owner.Id, events: events, objectBag: bag);
        await Assert.That(ReferenceEquals(metadata.TestBuilderContext.Current.Events, events)).IsTrue();
        await Assert.That(ReferenceEquals(metadata.TestBuilderContext.Current.StateBag, bag)).IsTrue();
    }

    [Test]
    public async Task Reflection_Static_Properties_Reinitialize_For_The_Next_Session()
    {
        var first = CreateSession();
        await StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(ReflectionFixture));
        var firstResource = ReflectionFixture.Resource!;
        await StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(ReflectionFixture));
        await Assert.That(ReferenceEquals(firstResource, ReflectionFixture.Resource)).IsTrue();

        await CreateHandler(first).DisposeStaticPropertiesAsync([], []);
        await Assert.That(firstResource.Disposed).IsTrue();

        var second = CreateSession();
        await StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(ReflectionFixture));
        var secondResource = ReflectionFixture.Resource!;
        await Assert.That(ReferenceEquals(firstResource, secondResource)).IsFalse();
        await Assert.That(secondResource.Disposed).IsFalse();
        await CreateHandler(second).DisposeStaticPropertiesAsync([], []);
        await Assert.That(secondResource.Disposed).IsTrue();
    }

    [Test]
    [NotInParallel]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Overlapping_Reflection_Sessions_Keep_Static_Resources_Alive(bool finishFirstSessionFirst)
    {
        var first = CreateSession();
        await StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(OverlappingReflectionFixture));
        var resource = OverlappingReflectionFixture.Resource!;

        var second = CreateSession();
        await StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(OverlappingReflectionFixture));
        await Assert.That(ReferenceEquals(resource, OverlappingReflectionFixture.Resource)).IsTrue();
        // Repeated discovery in one session must not acquire another claim.
        await StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(OverlappingReflectionFixture));

        var earlier = finishFirstSessionFirst ? first : second;
        var later = finishFirstSessionFirst ? second : first;
        await CreateHandler(earlier).DisposeStaticPropertiesAsync([], []);
        await Assert.That(resource.Disposed).IsFalse();
        await Assert.That(ReferenceEquals(resource, OverlappingReflectionFixture.Resource)).IsTrue();
        await CreateHandler(later).DisposeStaticPropertiesAsync([], []);
        await Assert.That(resource.Disposed).IsTrue();
        await Assert.That(resource.DisposeCount).IsEqualTo(1);

        var next = CreateSession();
        await StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(OverlappingReflectionFixture));
        var nextResource = OverlappingReflectionFixture.Resource!;
        await Assert.That(ReferenceEquals(resource, nextResource)).IsFalse();
        await Assert.That(nextResource.Disposed).IsFalse();
        await CreateHandler(next).DisposeStaticPropertiesAsync([], []);
        await Assert.That(nextResource.Disposed).IsTrue();
    }

    [Test]
    [NotInParallel]
    public async Task Failed_Reflection_Initialization_Releases_Its_Claim_And_Allows_A_Fresh_Session()
    {
        FailingReflectionFixture.FailNextAssignment = true;
        var first = CreateSession();
        await Assert.That(() => StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(FailingReflectionFixture)))
            .Throws<InvalidOperationException>();
        var failedResource = FailingReflectionFixture.Resource!;
        await Assert.That(failedResource.Disposed).IsFalse();
        // A repeated discovery in this same session must still report the original failure.
        await Assert.That(() => StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(FailingReflectionFixture)))
            .Throws<InvalidOperationException>();

        // Provider disposal uses this path when initialization fails before scheduling.
        await CreateHandler(first).ReleaseSessionCallbacksAfterFailedInitializationAsync();
        await Assert.That(failedResource.DisposeCount).IsEqualTo(1);
        await CreateHandler(first).ReleaseSessionCallbacksAfterFailedInitializationAsync();
        await Assert.That(failedResource.DisposeCount).IsEqualTo(1);

        var next = CreateSession();
        await StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(FailingReflectionFixture));
        var nextResource = FailingReflectionFixture.Resource!;
        await Assert.That(ReferenceEquals(failedResource, nextResource)).IsFalse();
        await Assert.That(nextResource.Disposed).IsFalse();
        await CreateHandler(next).DisposeStaticPropertiesAsync([], []);
        await Assert.That(nextResource.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task Throwing_Reflection_Release_Continues_Resource_And_Session_Cleanup()
    {
        var session = CreateSession();
        await StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(ThrowingReflectionFixture));
        var resource = ThrowingReflectionFixture.Resource!;
        var laterSessionCallback = 0;
        session.StaticPropertyContext.Events.OnDispose += (_, _) => { laterSessionCallback++; return default; };
        var errors = new List<Exception>();
        await Assert.That(() => CreateHandler(session).DisposeStaticPropertiesAsync(errors, []))
            .Throws<AggregateException>();
        await Assert.That(resource.DisposeCount).IsEqualTo(1);
        await Assert.That(laterSessionCallback).IsEqualTo(1);
        await Assert.That(errors.Count).IsEqualTo(1);
        await CreateHandler(session).DisposeStaticPropertiesAsync([], []);
        await Assert.That(resource.DisposeCount).IsEqualTo(1);

        var next = CreateSession();
        await StaticPropertyReflectionInitializer.InitializeStaticPropertiesForType(typeof(ThrowingReflectionFixture));
        await Assert.That(ReferenceEquals(resource, ThrowingReflectionFixture.Resource)).IsFalse();
        await Assert.That(() => CreateHandler(next).DisposeStaticPropertiesAsync([], []))
            .Throws<AggregateException>();
        await Assert.That(ThrowingReflectionFixture.Resource!.DisposeCount).IsEqualTo(1);
    }

    [Test]
    [NotInParallel]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Overlapping_Generated_Sessions_Keep_Fixtures_And_Scopes_Alive(bool finishFirstSessionFirst)
    {
        LeaseFixture? value = null;
        var initializations = 0;
        var metadata = new StaticPropertyMetadata
        {
            DeclaringType = typeof(LeaseFixture), PropertyName = "GeneratedLease", PropertyType = typeof(LeaseFixture),
            InitializerAsync = () =>
            {
                initializations++;
                var resource = value = new LeaseFixture();
                TestSessionContext.GlobalStaticPropertyContext.Events.OnDispose += (_, _) =>
                {
                    resource.ScopeDisposed = true;
                    resource.ScopeDisposeCount++;
                    return default;
                };
                return Task.FromResult<object?>(resource);
            }
        };
        var first = CreateSession();
        var firstHandler = CreateInitializingHandler(first);
        await firstHandler.InitializeStaticPropertiesAsync([metadata], default);
        var resource = value!;
        await Assert.That(ObjectTracker.IsExternallyOwned(resource)).IsFalse();
        var second = CreateSession();
        var secondHandler = CreateInitializingHandler(second);
        await secondHandler.InitializeStaticPropertiesAsync([metadata], default);
        await secondHandler.InitializeStaticPropertiesAsync([metadata], default);
        await Assert.That(initializations).IsEqualTo(1);
        await Assert.That(ReferenceEquals(resource, value)).IsTrue();

        // A session's global tracker sweep must not dispose a fixture leased by another session.
        var provider = new TrackableObjectGraphProvider(new FixtureDiscoverer(resource));
        var tracker = new ObjectTracker(provider, GlobalContext.Current.Disposer);
        var current = TestContext.Current!;
        var context = new TestContext(nameof(StaticPropertyHandlerCleanupTests), current.ServiceProvider, current.ClassContext,
            new TestBuilderContext { TestMetadata = current.TestDetails.MethodMetadata }, CancellationToken.None);
        tracker.TrackObjects(context);
        var earlier = finishFirstSessionFirst ? firstHandler : secondHandler;
        var later = finishFirstSessionFirst ? secondHandler : firstHandler;
        await earlier.DisposeStaticPropertiesAsync([], []);
        await tracker.UntrackObjects(context);
        // Exercise the same sweep operation on these fixtures without touching other tests' trackers.
        await tracker.DisposeUnleasedTrackedObjectAsync(resource);
        await tracker.DisposeUnleasedTrackedObjectAsync(resource.Child);
        await Assert.That(resource.Child.DisposeCount).IsEqualTo(0);
        await Assert.That(resource.Child.InitializeCount).IsEqualTo(1);
        await Assert.That(resource.DisposeCount).IsEqualTo(0);
        await Assert.That(resource.ScopeDisposeCount).IsEqualTo(0);
        await later.DisposeStaticPropertiesAsync([], []);
        await Assert.That(resource.DisposeCount).IsEqualTo(1);
        await Assert.That(resource.DisposedWithLiveScope).IsTrue();
        await Assert.That(resource.DisposedWithLiveChild).IsTrue();
        await Assert.That(resource.Child.DisposeCount).IsEqualTo(1);
        await Assert.That(resource.ScopeDisposeCount).IsEqualTo(1);
        await Assert.That(StaticPropertyRegistry.TryGetInitializedValue(typeof(LeaseFixture), "GeneratedLease", out _)).IsFalse();
        await later.ReleaseSessionCallbacksAfterFailedInitializationAsync();
        await Assert.That(resource.DisposeCount).IsEqualTo(1);

        var next = CreateInitializingHandler(CreateSession());
        await next.InitializeStaticPropertiesAsync([metadata], default);
        await Assert.That(initializations).IsEqualTo(2);
        await Assert.That(ReferenceEquals(resource, value)).IsFalse();
        await next.ReleaseSessionCallbacksAfterFailedInitializationAsync();
        await Assert.That(value!.DisposeCount).IsEqualTo(1);
        await Assert.That(value.ScopeDisposeCount).IsEqualTo(1);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Generated_Initialization_Failure_And_External_Ownership_Cleanup(bool externallyOwned)
    {
        var resource = new LeaseFixture();
        if (externallyOwned) ObjectTracker.MarkExternallyOwned(resource);
        var scopeDisposals = 0;
        var metadata = new StaticPropertyMetadata
        {
            DeclaringType = typeof(LeaseFixture), PropertyName = "FailedLease", PropertyType = typeof(LeaseFixture),
            InitializerAsync = () =>
            {
                TestSessionContext.GlobalStaticPropertyContext.Events.OnDispose += (_, _) =>
                {
                    scopeDisposals++;
                    if (externallyOwned) resource.Dispose();
                    return default;
                };
                return Task.FromResult<object?>(resource);
            }
        };
        // Failure after the generated initializer returned must release both fixture and scope.
        var handler = new StaticPropertyHandler(null!, new ObjectTracker(new(), GlobalContext.Current.Disposer), null!,
            new Lazy<PropertyInjector>(() => throw new InvalidOperationException("property injection failed")), null!, CreateSession());
        await Assert.That(() => handler.InitializeStaticPropertiesAsync([metadata], default)).Throws<InvalidOperationException>();
        await Assert.That(() => handler.InitializeStaticPropertiesAsync([metadata], default)).Throws<InvalidOperationException>();
        await handler.ReleaseSessionCallbacksAfterFailedInitializationAsync();
        await handler.ReleaseSessionCallbacksAfterFailedInitializationAsync();
        await Assert.That(resource.DisposeCount).IsEqualTo(1);
        await Assert.That(scopeDisposals).IsEqualTo(1);
    }

    [Test]
    public async Task Concurrent_Static_Lease_Acquisition_And_Sweeps_Keep_The_Value_Alive()
    {
        var resource = new LeaseFixture();
        var tracker = new ObjectTracker(new(), GlobalContext.Current.Disposer);
        tracker.TrackStaticLeaseObject(resource);
        await Task.WhenAll(Enumerable.Range(0, 512).Select(_ => Task.Run(async () =>
        {
            tracker.TrackStaticLeaseObject(resource);
            await tracker.DisposeUnleasedTrackedObjectAsync(resource);
            await tracker.ReleaseStaticLeaseObjectAsync(resource);
        })));
        await Assert.That(resource.DisposeCount).IsEqualTo(0);
        await tracker.ReleaseStaticLeaseObjectAsync(resource);
        await Assert.That(resource.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task Sweep_Cannot_Dispose_Nested_Fixture_During_Async_Static_Teardown()
    {
        var resource = new AsyncLeaseFixture();
        var metadata = new StaticPropertyMetadata
        {
            DeclaringType = typeof(AsyncLeaseFixture), PropertyName = "AsyncLease", PropertyType = typeof(AsyncLeaseFixture),
            InitializerAsync = () => Task.FromResult<object?>(resource)
        };
        var handler = CreateInitializingHandler(CreateSession());
        await handler.InitializeStaticPropertiesAsync([metadata], default);
        var release = handler.ReleaseSessionCallbacksAfterFailedInitializationAsync().AsTask();
        await resource.DisposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            var tracker = new ObjectTracker(new(), GlobalContext.Current.Disposer);
            await tracker.DisposeUnleasedTrackedObjectAsync(resource);
            await tracker.DisposeUnleasedTrackedObjectAsync(resource.Child);
            await Assert.That(resource.Child.DisposeCount).IsEqualTo(0);
        }
        finally
        {
            resource.FinishDisposal.SetResult();
            await release;
        }
        await Assert.That(resource.DisposedWithLiveChild).IsTrue();
        await Assert.That(resource.Child.DisposeCount).IsEqualTo(1);
    }

    [Test]
    [NotInParallel]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task Static_Properties_Sharing_A_Fixture_Retain_Its_Injected_Scope(bool shareNestedFixture, bool teardownThrows)
    {
        var shared = new SharedScopeFixture { TeardownThrows = teardownThrows };
        object firstValue = shareNestedFixture ? new SharedScopeRoot(shared) : shared;
        object secondValue = shareNestedFixture ? new SharedScopeRoot(shared) : shared;
        StaticPropertyMetadata Metadata(string name, object value) => new()
        {
            DeclaringType = typeof(SharedScopeFixture), PropertyName = name, PropertyType = value.GetType(),
            InitializerAsync = () => Task.FromResult<object?>(value)
        };
        var first = CreateInitializingHandler(CreateSession());
        await InitializeWithoutAmbientTestAsync(first, Metadata("SharedFirst", firstValue));
        var service = shared.Service;
        var second = CreateInitializingHandler(CreateSession());
        await InitializeWithoutAmbientTestAsync(second, Metadata("SharedSecond", secondValue));
        await Assert.That(ReferenceEquals(service, shared.Service)).IsTrue();

        await first.DisposeStaticPropertiesAsync([], []);
        await Assert.That(shared.DisposeCount).IsEqualTo(0);
        await Assert.That(service.DisposeCount).IsEqualTo(0);
        if (teardownThrows)
        {
            await Assert.That(() => second.DisposeStaticPropertiesAsync([], [])).Throws<AggregateException>();
        }
        else
        {
            await second.DisposeStaticPropertiesAsync([], []);
        }
        await Assert.That(shared.DisposeCount).IsEqualTo(1);
        await Assert.That(shared.DisposedWithLiveService).IsTrue();
        await Assert.That(service.DisposeCount).IsEqualTo(1);
        await first.ReleaseSessionCallbacksAfterFailedInitializationAsync();
        await second.ReleaseSessionCallbacksAfterFailedInitializationAsync();
        await Assert.That(service.DisposeCount).IsEqualTo(1);
    }

    [Test]
    [NotInParallel]
    public async Task Static_Callback_Registration_And_Fallback_Transfers_Share_Synchronization()
    {
        var previous = TestSessionContext.Current;
        TestSessionContext.Current = null;
        var context = TestSessionContext.GlobalStaticPropertyContext;
        var count = 0;
        try
        {
            await Task.WhenAll(Enumerable.Range(0, 512).Select(i => Task.Run(() =>
            {
                if (i % 2 == 0)
                {
                    TestSessionContext.RegisterStaticOnDispose(context, (_, _) => { Interlocked.Increment(ref count); return default; });
                }
                else
                {
                    var source = new TestContextEvents();
                    source.OnDispose += (_, _) => { Interlocked.Increment(ref count); return default; };
                    TestSessionContext.DeferOnDisposeToSessionEnd(source);
                }
            })));
            var errors = new List<Exception>();
            await TUnit.Engine.Services.TestExecution.TestOnDisposeRaiser.RaiseForSessionAsync(context, errors);
            await Assert.That(errors).IsEmpty();
            await Assert.That(count).IsEqualTo(512);
        }
        finally
        {
            TestSessionContext.Current = previous;
        }
    }

    internal sealed class SharedScopeRoot(SharedScopeFixture child) : IDisposable
    {
        [ClassDataSource<SharedScopeFixture>]
        public SharedScopeFixture Child { get; set; } = child;
        public void Dispose() { }
    }

    internal sealed class SharedScopeFixture : IDisposable
    {
        [SharedScope]
        public SharedScopeService Service { get; set; } = null!;
        public int DisposeCount;
        public bool DisposedWithLiveService;
        public bool TeardownThrows;
        public void Dispose()
        {
            DisposedWithLiveService = Service.DisposeCount == 0;
            DisposeCount++;
            if (TeardownThrows) throw new InvalidOperationException("shared fixture teardown failed");
        }
    }

    public sealed class SharedScopeService : IDisposable
    {
        public int DisposeCount;
        public void Dispose() => DisposeCount++;
    }

    public sealed class SharedScopeAttribute : DependencyInjectionDataSourceAttribute<SharedScopeService>
    {
        public override SharedScopeService CreateScope(DataGeneratorMetadata metadata) => new();
        public override object Create(SharedScopeService scope, Type type) => scope;
    }

    private sealed class AsyncLeaseFixture : IAsyncDisposable
    {
        public NestedLeaseFixture Child { get; } = new();
        public readonly TaskCompletionSource DisposalStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource FinishDisposal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool DisposedWithLiveChild;
        public async ValueTask DisposeAsync()
        {
            DisposalStarted.SetResult();
            await FinishDisposal.Task;
            DisposedWithLiveChild = Child.DisposeCount == 0;
        }
    }

    private sealed class LeaseFixture : IDisposable
    {
        public NestedLeaseFixture Child { get; } = new();
        public bool ScopeDisposed;
        public int ScopeDisposeCount;
        public int DisposeCount;
        public bool DisposedWithLiveScope;
        public bool DisposedWithLiveChild;
        public void Dispose()
        {
            DisposedWithLiveScope = !ScopeDisposed;
            DisposedWithLiveChild = Child.DisposeCount == 0;
            DisposeCount++;
        }
    }

    private sealed class NestedLeaseFixture : IDisposable, IAsyncInitializer
    {
        public int DisposeCount;
        public int InitializeCount;
        public Task InitializeAsync() { InitializeCount++; return Task.CompletedTask; }
        public void Dispose() { DisposeCount++; }
    }

    private static async Task InitializeWithoutAmbientTestAsync(StaticPropertyHandler handler, StaticPropertyMetadata metadata)
    {
        // Engine static initialization runs before any test is current. Avoid attaching the
        // injected scope to this unit test's OnDispose instead of the static value's context.
        var previous = TestContext.Current;
        var activity = System.Diagnostics.Activity.Current;
        AmbientContexts.SetTestOnly(null);
        System.Diagnostics.Activity.Current = null;
        try { await handler.InitializeStaticPropertiesAsync([metadata], default); }
        finally
        {
            AmbientContexts.SetTestOnly(previous);
            System.Diagnostics.Activity.Current = activity;
        }
    }

    private sealed class PassthroughInitializationCallback : IInitializationCallback
    {
        public ValueTask<T> EnsureInitializedAsync<T>(T obj,
            System.Collections.Concurrent.ConcurrentDictionary<string, object?>? objectBag = null,
            MethodMetadata? methodMetadata = null, TestContextEvents? events = null,
            CancellationToken cancellationToken = default) where T : notnull => new(obj);
    }

    private static StaticPropertyHandler CreateInitializingHandler(TestSessionContext session)
        => new(null!, new ObjectTracker(new(), GlobalContext.Current.Disposer), null!, new Lazy<PropertyInjector>(() => new PropertyInjector(new Lazy<IInitializationCallback>(() => new PassthroughInitializationCallback()), session.Id)),
            new ObjectGraphDiscoveryService(), session);

    internal sealed class ThrowingReflectionFixture
    {
        [ResourceSource(ThrowOnDispose = true)]
        public static Resource? Resource { get; set; }
    }

    internal sealed class FailingReflectionFixture
    {
        public static bool FailNextAssignment;
        private static Resource? _resource;

        [ResourceSource]
        public static Resource? Resource
        {
            get => _resource;
            set
            {
                _resource = value;
                if (FailNextAssignment)
                {
                    FailNextAssignment = false;
                    throw new InvalidOperationException("static initialization failed");
                }
            }
        }
    }

    internal sealed class OverlappingReflectionFixture
    {
        [ResourceSource]
        public static Resource? Resource { get; set; }
    }

    internal sealed class ReflectionFixture
    {
        [ResourceSource]
        public static Resource? Resource { get; set; }
    }

    public sealed class Resource
    {
        public bool Disposed { get; set; }
        public int DisposeCount { get; set; }
    }

    public sealed class ResourceSourceAttribute : Attribute, IDataSourceAttribute
    {
        public bool ThrowOnDispose { get; set; }
        public bool SkipIfEmpty { get; set; }
        public bool DeferEnumeration { get; set; }
        public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
        {
            var resource = new Resource();
            if (ThrowOnDispose)
            {
                metadata.TestBuilderContext.Current.Events.OnDispose += (_, _) =>
                    throw new InvalidOperationException("resource cleanup failed");
            }
            metadata.TestBuilderContext.Current.Events.OnDispose += (_, _) =>
            {
                resource.Disposed = true;
                resource.DisposeCount++;
                return default;
            };
            yield return () => Task.FromResult<object?[]?>([resource]);
            await Task.CompletedTask;
        }
    }

    private static StaticPropertyHandler CreateHandler(TestSessionContext session) => new(null!, null!, null!, null!, null!, session);

    private static TestSessionContext CreateSession() => new(new TestDiscoveryContext(new BeforeTestDiscoveryContext { TestFilter = null }) { TestFilter = null })
    {
        Id = Guid.NewGuid().ToString(), TestFilter = null
    };

    private sealed class Fixture : IDisposable
    {
        public bool Throws { get; init; }
        public bool ScopeDisposed { get; set; }
        public bool DisposedWithLiveScope { get; private set; }
        public void Dispose()
        {
            DisposedWithLiveScope = !ScopeDisposed;
            if (Throws) throw new InvalidOperationException("fixture teardown failed");
        }
    }

    private sealed class FixtureDiscoverer(object fixture) : IObjectGraphDiscoverer
    {
        public SortedList<int, HashSet<object>> DiscoverAndTrackObjects(TestContext context, CancellationToken cancellationToken = default)
        {
            context.TrackedObjects[0] = [fixture];
            return context.TrackedObjects;
        }
        public SortedList<int, HashSet<object>> TrackRootObjects(TestContext context) => context.TrackedObjects;
        public ObjectGraph DiscoverObjectGraph(TestContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ObjectGraph DiscoverNestedObjectGraph(object root, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
