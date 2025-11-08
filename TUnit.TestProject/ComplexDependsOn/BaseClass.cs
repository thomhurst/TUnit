using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.ComplexDependsOn;

[EngineTest(ExpectedResult.Pass)]
public class BaseClass
{
    [Test]
    public async Task Test1()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(50));
    }

    [Test]
    public async Task Test2()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(50));
    }

    [Test]
    public async Task Test3()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(50));
    }

    [Test]
    public async Task Test4()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(50));
    }

    [Test]
    public async Task Test5()
    {
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(50));
    }
}
