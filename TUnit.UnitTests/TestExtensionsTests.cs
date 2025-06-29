using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;
using TestContext = TUnit.Core.TestContext;

namespace TUnit.UnitTests;

public class TestExtensionsTests
{
    private readonly Fixture _fixture = new();

    [Test]
    public async Task TopLevelClass()
    {
        var testDetails = CreateTestDetails<TestExtensionsTests>();

        var context = CreateTestContext(testDetails);

        var name = context.GetClassTypeName();

        await Assert.That(name).IsEqualTo("TestExtensionsTests");
    }

    [Test]
    public async Task NestedClass()
    {
        var testDetails = CreateTestDetails<InnerClass>();

        var context = CreateTestContext(testDetails);

        var name = context.GetClassTypeName();

        await Assert.That(name).IsEqualTo("TestExtensionsTests+InnerClass");
    }

    private TestDetails<T> CreateTestDetails<T>() where T : class
    {
        var classMetadata = new ClassMetadata
        {
            Parent = ReflectionToSourceModelHelpers.GetParent(typeof(T)),
            Name = typeof(T).Name,
            Namespace = "TUnit.UnitTests",
            Assembly = new AssemblyMetadata
            {
                Attributes = [],
                Name = "TUnit.UnitTests",
            },
            Attributes = [],
            Parameters = [],
            Properties = [],
            Type = typeof(T),
            TypeReference = TypeReference.CreateConcrete($"{typeof(T).FullName}, {typeof(T).Assembly.GetName().Name}"),
        };

        return _fixture.Build<TestDetails<T>>()
            .OmitAutoProperties()
            .With(x => x.TestId, "test-id")
            .With(x => x.TestName, "TestName")
            .With(x => x.ClassType, typeof(T))
            .With(x => x.MethodName, "DummyMethod")
            .With(x => x.ClassInstance, null)
            .With(x => x.TestMethodArguments, Array.Empty<object?>())
            .With(x => x.TestClassArguments, [])
            .With(x => x.ReturnType, typeof(void))
            .With(x => x.ClassMetadata, classMetadata)
            .With(x => x.MethodMetadata, new MethodMetadata
            {
                Attributes = [],
                Class = classMetadata,
                Name = "DummyMethod",
                Parameters = [],
                Type = typeof(T),
                TypeReference = TypeReference.CreateConcrete($"{typeof(T).FullName}, {typeof(T).Assembly.GetName().Name}"),
                ReturnType = typeof(void),
                ReturnTypeReference = TypeReference.CreateConcrete($"{typeof(void).FullName}, {typeof(void).Assembly.GetName().Name}"),
                GenericTypeCount = 0,
            })
            .Create();
    }


    private TestContext CreateTestContext<
#if NET
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    T
    >(TestDetails<T> testDetails) where T : class
    {
        var testDiscoveryContext = new BeforeTestDiscoveryContext()
        {
            TestFilter = ""
        };
        var beforeTestDiscoveryContext = new TestDiscoveryContext(testDiscoveryContext)
        {
            TestFilter = ""
        };
        var testSessionContext = new TestSessionContext(beforeTestDiscoveryContext)
        {
            TestFilter = "",
            Id = "test-session-id",
        };
        var assemblyHookContext = new AssemblyHookContext(testSessionContext)
        {
            Assembly = typeof(T).Assembly
        };
        var classContext = new ClassHookContext(assemblyHookContext)
        {
            ClassType = typeof(T)
        };

        return _fixture.Build<TestContext>()
            .FromFactory(() => new TestContext(testDetails.TestName, testDetails.DisplayName ?? testDetails.TestName))
            .OmitAutoProperties()
            .With(x => x.TestDetails, testDetails)
            .With(x => x.ClassContext, classContext)
            .With(x => x.CancellationToken, CancellationToken.None)
            .Create();
    }


    public class InnerClass;
}
