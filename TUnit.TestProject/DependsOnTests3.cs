using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class DependsOnTests3
{
    private static DateTime _test1Start;
    private static DateTime _test2Start;
    private static DateTime _test3Start;

    [Test]
    public async Task Test1()
    {
        _test1Start = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    
    [Test]
    public async Task Test2()
    {
        _test2Start = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    
    [Test]
    [DependsOn(nameof(Test1))]
    public async Task Test3()
    {
        _test3Start = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    [AfterAllTestsInClass]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test3Start).Is.GreaterThanOrEqualTo(_test1Start.AddSeconds(1));
        await Assert.That(_test3Start).Is.GreaterThanOrEqualTo(_test2Start.AddSeconds(1));
    }
}