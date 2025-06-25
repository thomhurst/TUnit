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
        // TODO: Fix when TestStart property is added to TestContext or use Timings
        _test1Start = DateTime.Now; // TestContext.Current!.TestStart!.Value.DateTime;
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [Test, DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        // TODO: Fix when TestStart property is added to TestContext or use Timings
        _test2Start = DateTime.Now; // TestContext.Current!.TestStart!.Value.DateTime;
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test2Start).IsAfterOrEqualTo(_test1Start.AddSeconds(4.9));
    }
}