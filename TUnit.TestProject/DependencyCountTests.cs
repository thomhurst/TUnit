using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DependencyCountTests
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public Task Test1(int value)
    {
        return Task.CompletedTask;
    }

    [Test, DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        await Assert.That(TestContext.Current!.Dependencies).HasCount().EqualTo(3);
    }

    [Test]
    public Task Test3()
    {
        return Task.CompletedTask;
    }

    [Test, DependsOn(nameof(Test3))]
    public async Task Test4()
    {
        await Assert.That(TestContext.Current!.Dependencies).HasCount().EqualTo(1);
    }

    [Test, DependsOn(nameof(Test1))]
    public Task Test5()
    {
        return Task.CompletedTask;
    }

    [Test, DependsOn(nameof(Test5))]
    public async Task Test6()
    {
        await Assert.That(TestContext.Current!.Dependencies).HasCount().EqualTo(4);
    }
}
