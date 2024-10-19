using AutoFixture;
using TUnit.Core;
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
            .Without(x => x.ParallelLimit)
            .With(x => x.TestClassArguments, [])
            .Create();

        var context = new TestContext(testDetails, []);

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
            .Without(x => x.ParallelLimit)
            .With(x => x.TestClassArguments, [])
            .Create();

        var context = new TestContext(testDetails, []);

        var name = context.GetClassTypeName();
        
        Assert.That(name, Is.EqualTo("TestExtensionsTests+InnerClass"));
    }

    public class InnerClass;
}