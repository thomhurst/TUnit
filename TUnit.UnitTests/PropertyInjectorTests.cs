using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.UnitTests;

public class PropertyInjectorTests
{
    [Test]
    public async Task ReusedDiscoveryInstanceStillCachesInjectedProperties()
    {
        var context = CreateContext<ReusedDiscoveryInstanceTestClass>();
        context.IsDiscoveryInstanceReused = true;

        var injector = new PropertyInjector(new Lazy<IInitializationCallback>(() => new PassthroughInitializationCallback()), "session");

        await injector.ResolveAndCachePropertiesAsync(
            typeof(ReusedDiscoveryInstanceTestClass),
            context.StateBag.Items,
            context.Metadata.TestDetails.MethodMetadata,
            context.InternalEvents,
            context);

        await Assert.That(context.Metadata.TestDetails.TestClassInjectedPropertyArguments).Count().IsEqualTo(1);
        await Assert.That(context.Metadata.TestDetails.TestClassInjectedPropertyArguments.Values.Single())
            .IsTypeOf<ReusedDiscoveryInstanceFixture>();
    }

    private static TestContext CreateContext<T>() where T : class
    {
        var classMetadata = new ClassMetadata
        {
            Type = typeof(T),
            TypeInfo = new ConcreteType(typeof(T)),
            Name = typeof(T).Name,
            Namespace = typeof(T).Namespace ?? string.Empty,
            Assembly = new AssemblyMetadata
            {
                Name = typeof(T).Assembly.GetName().Name ?? string.Empty
            },
            Parent = null,
            Parameters = [],
            Properties = []
        };

        var methodMetadata = MethodMetadataFactory.Create(
            "Test",
            typeof(T),
            typeof(Task),
            classMetadata);

        var beforeDiscoveryContext = new BeforeTestDiscoveryContext { TestFilter = null };
        var discoveryContext = new TestDiscoveryContext(beforeDiscoveryContext) { TestFilter = null };
        var sessionContext = new TestSessionContext(discoveryContext)
        {
            Id = Guid.NewGuid().ToString(),
            TestFilter = null
        };
        var assemblyContext = new AssemblyHookContext(sessionContext)
        {
            Assembly = typeof(T).Assembly
        };
        var classContext = new ClassHookContext(assemblyContext)
        {
            ClassType = typeof(T)
        };
        var builderContext = new TestBuilderContext
        {
            TestMetadata = methodMetadata
        };

        var context = new TestContext("Test", new EmptyServiceProvider(), classContext, builderContext, CancellationToken.None);
        context.TestDetails = new TestDetails<T>([])
        {
            TestId = "Test",
            TestName = "Test",
            ClassType = typeof(T),
            MethodName = "Test",
            ClassInstance = PlaceholderInstance.Instance,
            TestMethodArguments = [],
            TestClassArguments = [],
            MethodMetadata = methodMetadata,
            ReturnType = typeof(Task),
            AttributesByType = new Dictionary<Type, IReadOnlyList<Attribute>>()
        };

        return context;
    }

    private sealed class ReusedDiscoveryInstanceFixture;

    private sealed class ReusedDiscoveryInstanceTestClass
    {
        [ClassDataSource<ReusedDiscoveryInstanceFixture>(Shared = SharedType.PerTestSession)]
        public required ReusedDiscoveryInstanceFixture Fixture { get; set; }
    }

    private sealed class PassthroughInitializationCallback : IInitializationCallback
    {
        public ValueTask<T> EnsureInitializedAsync<T>(
            T obj,
            ConcurrentDictionary<string, object?>? objectBag = null,
            MethodMetadata? methodMetadata = null,
            TestContextEvents? events = null,
            CancellationToken cancellationToken = default) where T : notnull
        {
            return ValueTask.FromResult(obj);
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
