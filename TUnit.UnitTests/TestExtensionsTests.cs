using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AutoFixture;
using TUnit.Assertions.Extensions;
using TUnit.Core;
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
        var testDetails = CreateTestDetails<InnerClass>(parent: typeof(TestExtensionsTests));

        var context = CreateTestContext(testDetails);

        var name = context.GetClassTypeName();

        await Assert.That(name).IsEqualTo("TestExtensionsTests+InnerClass");
    }

    private TestDetails<T> CreateTestDetails<T>(Type? parent = null) where T : class =>
        _fixture.Build<TestDetails<T>>()
            .OmitAutoProperties()
            .With(x => x.TestClassArguments, [])
            .With(x => x.TestMethod, new TestMethod
            {
                Attributes = [],
                Class = new TestClass
                {
                    Parent = parent == null ? null : ReflectionToSourceModelHelpers.GetParent(parent),
                    Name = typeof(T).Name,
                    Namespace = "TUnit.UnitTests",
                    Assembly = new TestAssembly
                    {
                        Attributes = [],
                        Name = "TUnit.UnitTests",
                    },
                    Attributes = [],
                    Parameters = [],
                    Properties = [],
                    Type = typeof(T),
                },
                Name = "DummyMethod",
                Parameters = [],
                Type = typeof(TestExtensionsTests),
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
        var constructor = typeof(TestContext).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(IServiceProvider), typeof(TestDetails), typeof(TestMetadata), typeof(ClassHookContext)],
            [])!;

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

        return (TestContext)constructor.Invoke([null, testDetails, CreateDummyMetadata(), classContext]);
    }

    private TestMetadata<TestExtensionsTests> CreateDummyMetadata()
    {
        return _fixture.Build<TestMetadata<TestExtensionsTests>>()
            .OmitAutoProperties()
            .With(x => x.DynamicAttributes, [])
            .With(x => x.TestMethod, new TestMethod
            {
                Attributes = [],
                Class = new TestClass
                {
                    Parent = ReflectionToSourceModelHelpers.GetParent(typeof(TestExtensionsTests)),
                    Name = "TestExtensionsTests",
                    Namespace = "TUnit.UnitTests",
                    Assembly = new TestAssembly
                    {
                        Attributes = [],
                        Name = "TUnit.UnitTests",
                    },
                    Attributes = [],
                    Parameters = [],
                    Properties = [],
                    Type = typeof(TestExtensionsTests),
                },
                Name = "DummyMethod",
                Parameters = [],
                Type = typeof(TestExtensionsTests),
                ReturnType = typeof(void),
                GenericTypeCount = 0,
            })
            .Without(x => x.ResettableClassFactory)
            .Create();
    }

    public class InnerClass;
}
