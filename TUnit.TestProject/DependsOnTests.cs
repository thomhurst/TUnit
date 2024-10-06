using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class DependsOnTests
{
    private static DateTime _test1Start;
    private static DateTime _test2Start;

    [Test]
    public async Task Test1()
    {
        _test1Start = TestContext.Current!.TestStart!.Value.DateTime;
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
    
    [Test, DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        _test2Start = TestContext.Current!.TestStart!.Value.DateTime;
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test2Start).IsAfterOrEqualTo(_test1Start.AddSeconds(4.9));
    }
}