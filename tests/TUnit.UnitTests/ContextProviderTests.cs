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

    private sealed class DummyTestClass
    {
        public Task SomeTest() => Task.CompletedTask;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
