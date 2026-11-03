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
