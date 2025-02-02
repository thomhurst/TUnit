using AutoFixture;
using TUnit.Core;
using TUnit.Core.Extensions;
using TestContext = TUnit.Core.TestContext;

namespace TUnit.UnitTests;

public class TestExtensionsTests
{
    private readonly Fixture _fixture = new();

    [Test]
    public void TopLevelClass()
    {
        var testDetails = _fixture.Build<TestDetails<TestExtensionsTests>>()
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
            .With(x => x.TestClassArguments, [])
            .Create();

        var context = new TestContext(null!, testDetails, CreateDummyMetadata());

        var name = context.GetClassTypeName();
        
        Assert.That(name, Is.EqualTo("TestExtensionsTests+InnerClass"));
    }

    private TestMetadata<TestExtensionsTests> CreateDummyMetadata()
    {
        return _fixture.Build<TestMetadata<TestExtensionsTests>>()
            .Without(x => x.ResettableClassFactory)
            .Create();
    }

    public class InnerClass;
}