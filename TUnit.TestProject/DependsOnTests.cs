using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class DependsOnTests
{
    private static DateTime test1Start;
    private static DateTime test2Start;

    [Test]
    public async Task Test1()
    {
        test1Start = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
    
    [Test, DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        test2Start = DateTime.Now;
        await Task.CompletedTask;
    }

    [AfterAllTestsInClass]
    public static async Task AssertStartTimes()
    {
        await Assert.That(test2Start).Is.GreaterThanOrEqualTo(test1Start.AddSeconds(5));
    }
}