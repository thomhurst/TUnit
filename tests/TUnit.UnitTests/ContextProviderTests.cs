namespace TUnit.UnitTests;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/6180 —
/// a partially-built <see cref="TestContext"/> (with <c>TestDetails == null</c>) must never
/// be observable via <see cref="ClassHookContext.Tests"/>, otherwise AfterEvery(Class) hooks
/// running concurrently with dynamic test registration NRE on <c>test.Metadata.TestDetails</c>.
/// </summary>
public class ContextProviderTests
{
    [Test]
    public async Task CreateTestContext_PublishesContextWithTestDetailsAlreadyAssigned()
    {
        var provider = new ContextProvider(new EmptyServiceProvider(), Guid.NewGuid().ToString(), testFilter: null);

        var classMetadata = new ClassMetadata
        {
            Type = typeof(DummyTestClass),
            TypeInfo = new ConcreteType(typeof(DummyTestClass)),
            Name = nameof(DummyTestClass),
            Namespace = typeof(DummyTestClass).Namespace ?? string.Empty,
            Assembly = new AssemblyMetadata
            {
                Name = typeof(DummyTestClass).Assembly.GetName().Name ?? string.Empty
            },
            Parent = null,
            Parameters = [],
            Properties = []
        };

        var methodMetadata = MethodMetadataFactory.Create(
            nameof(DummyTestClass.SomeTest),
            typeof(DummyTestClass),
            typeof(Task),
            classMetadata);

        var testDetails = new TestDetails([])
        {
            TestId = "Test:0",
            TestName = nameof(DummyTestClass.SomeTest),
            ClassType = typeof(DummyTestClass),
            MethodName = nameof(DummyTestClass.SomeTest),
            ClassInstance = PlaceholderInstance.Instance,
            TestMethodArguments = [],
            TestClassArguments = [],
            MethodMetadata = methodMetadata,
            ReturnType = typeof(Task),
            AttributesByType = new Dictionary<Type, IReadOnlyList<Attribute>>()
        };

        var context = provider.CreateTestContext(
            typeof(DummyTestClass),
            new TestBuilderContext { TestMetadata = methodMetadata },
            testDetails,
            CancellationToken.None);

        var classContext = provider.GetOrCreateClassContext(typeof(DummyTestClass));
        var publishedContext = classContext.Tests.Single();

        // The contract callers (and AfterEvery(Class) hooks) rely on: by the time a context is
        // visible in ClassHookContext.Tests, its TestDetails is set — no post-hoc assignment.
        await Assert.That(publishedContext).IsSameReferenceAs(context);
        await Assert.That(publishedContext.TestDetails).IsNotNull();
        await Assert.That(publishedContext.Metadata.TestDetails).IsSameReferenceAs(testDetails);
    }

    [Test]
    public async Task GetOrCreateClassContext_ConcurrentCallers_RegisterASingleContext()
    {
        // Contexts register themselves with their parent when constructed. Creating them through
        // a racing ConcurrentDictionary.GetOrAdd factory left the losers registered as orphaned,
        // test-less entries in AssemblyHookContext.TestClasses / TestSessionContext.Assemblies.
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var provider = new ContextProvider(new EmptyServiceProvider(), Guid.NewGuid().ToString(), testFilter: null);
            // The engine creates the session context before building tests; do the same so only
            // the class/assembly creation path races.
            var session = provider.TestSessionContext;
            using var start = new ManualResetEventSlim();

            var workers = Enumerable.Range(0, Math.Clamp(Environment.ProcessorCount, 4, 16))
                .Select(_ => Task.Factory.StartNew(() =>
                {
                    start.Wait();
                    return provider.GetOrCreateClassContext(typeof(DummyTestClass));
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default))
                .ToArray();

            start.Set();
            var contexts = await Task.WhenAll(workers);

            await Assert.That(contexts.All(c => ReferenceEquals(c, contexts[0]))).IsTrue();
            await Assert.That(session.Assemblies).Count().IsEqualTo(1);
            await Assert.That(session.TestClasses).Count().IsEqualTo(1);
        }
    }

    [Test]
    public async Task AddTest_AfterFilteringEmptiedClass_ReattachesClassAndAssembly()
    {
        // Filtering can remove every built test of a class, which detaches the class (and its
        // assembly) from the hook contexts. A dynamic test added to that class at run time must
        // make it visible to assembly and session hooks again.
        var provider = new ContextProvider(new EmptyServiceProvider(), Guid.NewGuid().ToString(), testFilter: null);
        var session = provider.TestSessionContext;

        CreateDummyTestContext(provider, "Test:0");
        var classContext = provider.GetOrCreateClassContext(typeof(DummyTestClass));

        classContext.RetainTests(new HashSet<TestContext>());

        await Assert.That(session.Assemblies).IsEmpty();
        await Assert.That(session.TestClasses).IsEmpty();

        var dynamicTest = CreateDummyTestContext(provider, "Test:1");

        await Assert.That(session.Assemblies).Count().IsEqualTo(1);
        await Assert.That(session.TestClasses).Count().IsEqualTo(1);
        await Assert.That(classContext.AssemblyContext.AllTests).Contains(dynamicTest);
        await Assert.That(session.AllTests).Contains(dynamicTest);
    }

    private static TestContext CreateDummyTestContext(ContextProvider provider, string testId)
    {
        var classMetadata = new ClassMetadata
        {
            Type = typeof(DummyTestClass),
            TypeInfo = new ConcreteType(typeof(DummyTestClass)),
            Name = nameof(DummyTestClass),
            Namespace = typeof(DummyTestClass).Namespace ?? string.Empty,
            Assembly = new AssemblyMetadata
            {
                Name = typeof(DummyTestClass).Assembly.GetName().Name ?? string.Empty
            },
            Parent = null,
            Parameters = [],
            Properties = []
        };

        var methodMetadata = MethodMetadataFactory.Create(
            nameof(DummyTestClass.SomeTest),
            typeof(DummyTestClass),
            typeof(Task),
            classMetadata);

        var testDetails = new TestDetails([])
        {
            TestId = testId,
            TestName = nameof(DummyTestClass.SomeTest),
            ClassType = typeof(DummyTestClass),
            MethodName = nameof(DummyTestClass.SomeTest),
            ClassInstance = PlaceholderInstance.Instance,
            TestMethodArguments = [],
            TestClassArguments = [],
            MethodMetadata = methodMetadata,
            ReturnType = typeof(Task),
            AttributesByType = new Dictionary<Type, IReadOnlyList<Attribute>>()
        };

        return provider.CreateTestContext(
            typeof(DummyTestClass),
            new TestBuilderContext { TestMetadata = methodMetadata },
            testDetails,
            CancellationToken.None);
    }

    private sealed class DummyTestClass
    {
        public Task SomeTest() => Task.CompletedTask;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
