using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3723;

public sealed class StateBagClassConstructor : IClassConstructor
{
    public Task<object> Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
        ClassConstructorMetadata classConstructorMetadata)
    {
        classConstructorMetadata.TestBuilderContext.StateBag["TestKey"] = "TestValue";
        classConstructorMetadata.TestBuilderContext.StateBag["Counter"] = 0;

        return Task.FromResult(Activator.CreateInstance(type)!);
    }
}

[EngineTest(ExpectedResult.Pass)]
[ClassConstructor<StateBagClassConstructor>]
public sealed class StateBagPropagationTests
{
    [Test]
    public async Task StateBag_Data_Should_Flow_From_ClassConstructor_To_TestContext()
    {
        await Assert.That(TestContext.Current!.StateBag["TestKey"])
            .IsEqualTo("TestValue");
    }

    [Test]
    public async Task Multiple_Tests_Should_Have_Isolated_StateBags()
    {
        var counter = (int)TestContext.Current!.StateBag["Counter"]!;
        counter++;
        TestContext.Current!.StateBag["Counter"] = counter;

        await Assert.That(counter).IsEqualTo(1);
    }

    [Test]
    public async Task Another_Test_Should_Also_Have_Isolated_StateBag()
    {
        var counter = (int)TestContext.Current!.StateBag["Counter"]!;
        counter++;
        TestContext.Current!.StateBag["Counter"] = counter;

        await Assert.That(counter).IsEqualTo(1);
    }
}
