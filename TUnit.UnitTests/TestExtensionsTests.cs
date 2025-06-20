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

    private TestDetails<T> CreateTestDetails<T>() where T : class =>
        _fixture.Build<TestDetails<T>>()
            .OmitAutoProperties()
            .With(x => x.TestClassArguments, [])
            .With(x => x.MethodMetadata, new MethodMetadata
            {
                Attributes = [],
                Class = new ClassMetadata
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
                    Constructors = [],
                    Type = typeof(T),
                },
                Name = "DummyMethod",
                Parameters = [],
                Type = typeof(T),
                ReturnType = typeof(void),
                GenericTypeCount = 0,
            })
            .Create();


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

        var testBuilderContext = new TestBuilderContext();
        var testDefinition = CreateDummyTestDefinition(testDetails);
        return _fixture.Build<TestContext>()
            .FromFactory(() => new TestContext(null!, testDetails, testDefinition, testBuilderContext, classContext))
            .OmitAutoProperties()
            .Create();
    }

    private TestDefinition CreateDummyTestDefinition<T>(TestDetails<T> testDetails) where T : class
    {
        return _fixture.Build<TestDefinition>()
            .OmitAutoProperties()
            .With(x => x.MethodMetadata, testDetails.MethodMetadata)
            .With(x => x.TestClassFactory, () => Activator.CreateInstance<T>())
            .With(x => x.TestMethodInvoker, (obj, ct) => new ValueTask())
            .With(x => x.ClassArgumentsProvider, () => Array.Empty<object?>())
            .With(x => x.MethodArgumentsProvider, () => Array.Empty<object?>())
            .With(x => x.PropertiesProvider, () => new Dictionary<string, object?>())
            .Create();
    }

    public class InnerClass;
}
