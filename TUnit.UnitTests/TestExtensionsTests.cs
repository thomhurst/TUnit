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
        var testDetails = _fixture.Build<TestDetails<TestExtensionsTests>>()
            .With(x => x.DynamicAttributes, [])
            .With(x => x.TestClassArguments, [])
            .With(x => x.DataAttributes, [])
            .With(x => x.TestMethod, new SourceGeneratedMethodInformation
            {
                Attributes = [],
                Class = new SourceGeneratedClassInformation
                {
                    Name = "TestExtensionsTests",
                    Namespace = "TUnit.UnitTests",
                    Assembly = new SourceGeneratedAssemblyInformation
                    {
                        Attributes = [],
                        Name = "TUnit.UnitTests",
                    },
                    Attributes = [],
                    Parameters = [],
                    Properties = [],
                    Type = typeof(TestExtensionsTests),
                    Parent = null,
                },
                Name = "DummyMethod",
                Parameters = [],
                Type = typeof(TestExtensionsTests),
                ReturnType = typeof(void),
                GenericTypeCount = 0,
            })
            .Create();

        var context = CreateTestContext(testDetails);

        var name = context.GetClassTypeName();
        
        await Assert.That(name).IsEqualTo("TestExtensionsTests");
    }

    [Test]
    public async Task NestedClass()
    {
        var testDetails = _fixture.Build<TestDetails<InnerClass>>()
            .With(x => x.DynamicAttributes, [])
            .With(x => x.TestClassArguments, [])
            .With(x => x.DataAttributes, [])
            .With(x => x.TestMethod, new SourceGeneratedMethodInformation
            {
                Attributes = [],
                Class = new SourceGeneratedClassInformation
                {
                    Parent = ReflectionToSourceModelHelpers.GetParent(typeof(InnerClass)),
                    Name = "InnerClass",
                    Namespace = "TUnit.UnitTests",
                    Assembly = new SourceGeneratedAssemblyInformation
                    {
                        Attributes = [],
                        Name = "TUnit.UnitTests",
                    },
                    Attributes = [],
                    Parameters = [],
                    Properties = [],
                    Type = typeof(InnerClass),
                },
                Name = "DummyMethod",
                Parameters = [],
                Type = typeof(TestExtensionsTests),
                ReturnType = typeof(void),
                GenericTypeCount = 0,
            })            .Create();

        var context = CreateTestContext(testDetails);

        var name = context.GetClassTypeName();
        
        await Assert.That(name).IsEqualTo("TestExtensionsTests+InnerClass");
    }

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
            [typeof(IServiceProvider), typeof(TestDetails), typeof(TestMetadata)],
            [])!;

        return (TestContext)constructor.Invoke([null, testDetails, CreateDummyMetadata()]);
    }

    private TestMetadata<TestExtensionsTests> CreateDummyMetadata()
    {
        return _fixture.Build<TestMetadata<TestExtensionsTests>>()
            .With(x => x.DynamicAttributes, [])
            .With(x => x.TestMethod, new SourceGeneratedMethodInformation
            {
                Attributes = [],
                Class = new SourceGeneratedClassInformation
                {
                    Parent = ReflectionToSourceModelHelpers.GetParent(typeof(TestExtensionsTests)),
                    Name = "TestExtensionsTests",
                    Namespace = "TUnit.UnitTests",
                    Assembly = new SourceGeneratedAssemblyInformation
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