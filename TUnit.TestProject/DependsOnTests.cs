using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DependsOnTests
{
    private static DateTime _test1Start;
    private static DateTime _test2Start;

    [Test]
    public async Task Test1()
    {
        _test1Start = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [Test, DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        _test2Start = DateTime.Now;
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test2Start).IsAfterOrEqualTo(_test1Start.AddSeconds(4.9));
    }
}

public sealed class MyAsyncTest
{
    public static int NumberOfInvocations = 0;

    [Test]
    public async Task Test()
    {
        NumberOfInvocations += 1;
        await Assert.That(NumberOfInvocations).IsEqualTo(1);
    }
}

[EngineTest(ExpectedResult.Pass)]
[DependsOn(typeof(MyAsyncTest), nameof(Test))]
public sealed class DependsOn_AsyncTest
{
    [Test]
    public async Task Test() => await Assert.That(MyAsyncTest.NumberOfInvocations).IsEqualTo(1);
}

[EngineTest(ExpectedResult.Pass)]
[DependsOn(typeof(MyAsyncTest), nameof(Test))]
public sealed class DependsOn_AsyncTest_Two
{
    [Test]
    public async Task Test() => await Assert.That(MyAsyncTest.NumberOfInvocations).IsEqualTo(1);
}
