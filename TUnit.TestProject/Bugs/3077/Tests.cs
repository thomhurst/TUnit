using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3077;

public sealed class ExampleConstructor : IClassConstructor, ITestStartEventReceiver, ITestEndEventReceiver
{
    public Task<object> Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ClassConstructorMetadata classConstructorMetadata)
    {
        return Task.FromResult<object>(new Tests());
    }

    public ValueTask OnTestStart(TestContext context)
    {
        ((Tests)context.TestDetails.ClassInstance).SomeValue = "Initialized";
        return default;
    }

    public ValueTask OnTestEnd(TestContext context)
    {
        return default;
    }

    public int Order
    {
        get;
    }
}

[EngineTest(ExpectedResult.Pass)]
[ClassConstructor<ExampleConstructor>]
public sealed class Tests
{
    public string SomeValue = "Uninitialized";

    [Test]
    public async Task Foo()
    {
        await Assert.That(SomeValue).IsEqualTo("Initialized");
    }
}
