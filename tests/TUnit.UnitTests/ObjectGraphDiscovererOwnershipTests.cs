using TUnit.Core.Discovery;
using TUnit.Core.Interfaces;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.PropertyInjection;
using TUnit.Core.Tracking;

namespace TUnit.UnitTests;

public class ObjectGraphDiscovererOwnershipTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public Task ScopeOwnedInitializerDependency_IsNotDisposedByTracker(bool sourceGenerated)
        => CheckOwnership(sourceGenerated, injected: false, alias: false);

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public Task InjectedInitializer_IsDisposedOnceByTracker(bool sourceGenerated)
        => CheckOwnership(sourceGenerated, injected: true, alias: false);

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public Task InjectedInitializer_AliasedByScopeOwnedDependency_RemainsTrackerOwned(bool sourceGenerated)
        => CheckOwnership(sourceGenerated, injected: true, alias: true);

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public Task InjectedInitializer_AliasedDirectlyByScopeOwnedService_RemainsTrackerOwned(bool sourceGenerated)
        => CheckOwnership(sourceGenerated, injected: true, alias: true, nestedAlias: false);

    private static Task CheckOwnership(bool sourceGenerated, bool injected, bool alias, bool nestedAlias = true)
        => sourceGenerated
            ? CheckOwnership<GeneratedMode>(injected, alias, true, nestedAlias)
            : CheckOwnership<ReflectionMode>(injected, alias, false, nestedAlias);

    private static async Task CheckOwnership<TMode>(bool injected, bool alias, bool sourceGenerated, bool nestedAlias)
    {
        // Closed generic fixture types avoid automatic generator registration. Register only the
        // generated-mode types, without clearing process-wide registries used by parallel tests.
        if (sourceGenerated)
        {
            PropertySourceRegistry.Register(typeof(Service<TMode>), new ServiceProperties<TMode>());
            InitializerPropertyRegistry.Register(typeof(Service<TMode>),
            [
                new() { PropertyName = nameof(Service<TMode>.Dependency), PropertyType = typeof(Probe<TMode>),
                    GetValue = obj => ((Service<TMode>)obj).Dependency },
                new() { PropertyName = nameof(Service<TMode>.Injected), PropertyType = typeof(Probe<TMode>),
                    GetValue = obj => ((Service<TMode>)obj).Injected }
            ]);
            InitializerPropertyRegistry.Register(typeof(Probe<TMode>),
            [
                new() { PropertyName = nameof(Probe<TMode>.Child), PropertyType = typeof(Probe<TMode>),
                    GetValue = obj => ((Probe<TMode>)obj).Child }
            ]);
        }

        await Assert.That(InitializerPropertyRegistry.HasRegistration(typeof(Service<TMode>))).IsEqualTo(sourceGenerated);
        await Assert.That(InitializerPropertyRegistry.HasRegistration(typeof(Probe<TMode>))).IsEqualTo(sourceGenerated);
        var plan = PropertyInjectionCache.GetOrCreatePlan(typeof(Service<TMode>));
        // Reflection execution deliberately ignores registered property sources. The generated
        // fixture still exercises initializer metadata, while injection uses the selected mode.
        var useGeneratedProperties = sourceGenerated && SourceRegistrar.IsEnabled;
        await Assert.That(plan.SourceGeneratedProperties.Length).IsEqualTo(useGeneratedProperties ? 1 : 0);
        await Assert.That(plan.ReflectionProperties.Length).IsEqualTo(useGeneratedProperties ? 0 : 1);

        var value = new Probe<TMode>();
        var dependency = alias
            ? (nestedAlias ? new Probe<TMode> { Child = value } : value)
            : (injected ? null : value);
        var service = new Service<TMode> { Dependency = dependency, Injected = injected ? value : null };
        var current = TestContext.Current!;
        var builder = new TestBuilderContext { TestMetadata = current.TestDetails.MethodMetadata };
        var scopes = builder.Events.GetOrCreateDataRowScopes();
        scopes.RegisterObject(scopes.Register(new Scope()), service);
        scopes.CaptureOwnership();
        var context = new TestContext("Ownership", current.ServiceProvider, current.ClassContext,
            builder, CancellationToken.None);
        context.Metadata.TestDetails = new TestDetails([])
        {
            TestId = context.Id, TestName = "Ownership", ClassType = typeof(ObjectGraphDiscovererOwnershipTests),
            MethodName = "Ownership", ClassInstance = new ObjectGraphDiscovererOwnershipTests(), TestMethodArguments = [], TestClassArguments = [service],
            MethodMetadata = current.TestDetails.MethodMetadata, ReturnType = typeof(void),
            AttributesByType = new Dictionary<Type, IReadOnlyList<Attribute>>()
        };
        var discoverer = new ObjectGraphDiscoverer();
        var tracker = new ObjectTracker(new TrackableObjectGraphProvider(discoverer), GlobalContext.Current.Disposer);
        try
        {
            var graph = discoverer.DiscoverAndTrackObjects(context);
            await Assert.That(graph.Values.SelectMany(bucket => bucket).Any(obj => ReferenceEquals(obj, value))).IsTrue();
            await Assert.That(ExternalObjectOwnership.IsOwned(value)).IsEqualTo(!injected);
            if (alias && nestedAlias)
            {
                await Assert.That(ExternalObjectOwnership.IsOwned(dependency!)).IsTrue();
            }

            context.TrackedObjects.Clear();
            tracker.TrackObjects(context);
            await Assert.That(value.DisposeCount).IsEqualTo(0);
            await Assert.That(await tracker.UntrackObjects(context)).IsNull();
            await Assert.That(await tracker.DisposeAndClearStaticTrackingAsync()).IsNull();
            await Assert.That(value.DisposeCount).IsEqualTo(injected ? 1 : 0);
            await Assert.That(service.DisposeCount).IsEqualTo(0);
            if (alias && nestedAlias)
            {
                await Assert.That(dependency!.DisposeCount).IsEqualTo(0);
            }

            // Simulate the external scope's cleanup after the tracker has released its references.
            service.Dispose();
            await Assert.That(value.DisposeCount).IsEqualTo(1);
            await Assert.That(service.DisposeCount).IsEqualTo(1);
        }
        finally
        {
            await tracker.DisposeAndClearStaticTrackingAsync();
            context.RemoveFromRegistry();
            context.Dispose();
        }
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose() { }
    }

    private sealed class GeneratedMode;
    private sealed class ReflectionMode;

    private sealed class Probe<TMode> : IAsyncInitializer, IDisposable
    {
        public Probe<TMode>? Child { get; init; }
        public int DisposeCount { get; private set; }
        public Task InitializeAsync() => Task.CompletedTask;
        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed class Service<TMode> : IDisposable
    {
        public Probe<TMode>? Dependency { get; init; }
        [MethodDataSource(nameof(CreateProbe))]
        public Probe<TMode>? Injected { get; init; }
        public static Probe<TMode> CreateProbe() => new();
        public int DisposeCount { get; private set; }
        public void Dispose()
        {
            DisposeCount++;
            // An injected value stays owned by TUnit even when exposed through a service alias.
            if (!ReferenceEquals(Dependency, Injected))
            {
                Dependency?.Dispose();
            }
        }
    }

    private sealed class ServiceProperties<TMode> : IPropertySource
    {
        public Type Type => typeof(Service<TMode>);
        public bool ShouldInitialize => true;
        public IEnumerable<PropertyInjectionMetadata> GetPropertyMetadata() =>
        [
            new()
            {
                PropertyName = nameof(Service<TMode>.Injected), PropertyType = typeof(Probe<TMode>),
                ContainingType = typeof(Service<TMode>), CreateDataSource = () => new MethodDataSourceAttribute(nameof(Service<TMode>.CreateProbe)),
                GetProperty = obj => ((Service<TMode>)obj).Injected,
                SetProperty = (_, _) => throw new NotSupportedException("Discovery only reads injected properties")
            }
        ];
    }
}
