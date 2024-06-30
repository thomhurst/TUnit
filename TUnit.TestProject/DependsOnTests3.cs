using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class DependsOnTests3
{
    private static DateTime test1Start;
    private static DateTime test2Start;
    private static DateTime test3Start;

    [Test]
    public async Task Test1()
    {
        test1Start = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    
    [Test]
    public async Task Test2()
    {
        test2Start = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    
    [Test]
    [DependsOn(nameof(Test1))]
    [DependsOn(nameof(Test2))]
    public async Task Test3()
    {
        test3Start = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    [AfterAllTestsInClass]
    public static async Task AssertStartTimes()
    {
        await Assert.That(test3Start).Is.GreaterThanOrEqualTo(test1Start.AddSeconds(1));
        await Assert.That(test3Start).Is.GreaterThanOrEqualTo(test2Start.AddSeconds(1));
    }
}