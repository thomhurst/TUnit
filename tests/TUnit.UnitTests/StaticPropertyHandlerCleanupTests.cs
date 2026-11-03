using TUnit.Core.Discovery;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;
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
        public bool SkipIfEmpty { get; set; }
        public bool DeferEnumeration { get; set; }
        public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
        {
            var resource = new Resource();
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
