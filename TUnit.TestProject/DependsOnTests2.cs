using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class DependsOnTests2
{
    private static DateTime test1Start;
    private static DateTime test2Start;

    [DataDrivenTest]
    [Arguments("1", 2, true)]
    public async Task Test1(string one, int two, bool three)
    {
        test1Start = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
    
    [Test, DependsOn(nameof(Test1), parameterTypes: [typeof(string), typeof(int), typeof(bool)])]
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