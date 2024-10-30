using AutoFixture;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Engine.Extensions;
using TestContext = TUnit.Core.TestContext;

namespace TUnit.UnitTests;

public class TestExtensionsTests
{
    private readonly Fixture _fixture = new();

    [Test]
    public void TopLevelClass()
    {
        var testDetails = _fixture.Build<TestDetails<TestExtensionsTests>>()
            .Without(x => x.ClassType)
            .Without(x => x.MethodInfo)
            .Without(x => x.Attributes)
            .Without(x => x.AssemblyAttributes)
            .Without(x => x.ClassAttributes)
            .Without(x => x.DataAttributes)
            .Without(x => x.TestAttributes)
            .With(x => x.TestClassArguments, [])
            .Create();

        var context = new TestContext(null!, testDetails, CreateDummyMetadata());

        var name = context.GetClassTypeName();
        
        Assert.That(name, Is.EqualTo("TestExtensionsTests"));
    }

    [Test]
    public void NestedClass()
    {
        var testDetails = _fixture.Build<TestDetails<InnerClass>>()
            .Without(x => x.ClassType)
            .Without(x => x.MethodInfo)
            .Without(x => x.Attributes)
            .Without(x => x.AssemblyAttributes)
            .Without(x => x.ClassAttributes)
            .Without(x => x.DataAttributes)
            .Without(x => x.TestAttributes)
            .With(x => x.TestClassArguments, [])
            .Create();

        var context = new TestContext(null!, testDetails, CreateDummyMetadata());

        var name = context.GetClassTypeName();
        
        Assert.That(name, Is.EqualTo("TestExtensionsTests+InnerClass"));
    }

    private TestMetadata<TestExtensionsTests> CreateDummyMetadata()
    {
        return _fixture.Build<TestMetadata<TestExtensionsTests>>()
            .Without(x => x.MethodInfo)
            .Without(x => x.ResettableClassFactory)
            .With(x => x.AttributeTypes, [])
            .With(x => x.DataAttributes, [])
            .Create();
    }

    public class InnerClass;
}