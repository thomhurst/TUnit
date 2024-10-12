using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Extensions;

namespace TUnit.TestProject;

public class DependsOnTests2
{
    private static DateTime _test1Start;
    private static DateTime _test2Start;

    [Test]
    [Arguments("1", 2, true)]
    public async Task Test1(string one, int two, bool three)
    {
        _test1Start = TestContext.Current!.TestStart!.Value.DateTime;
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
    
    [Test, DependsOn(nameof(Test1), parameterTypes: [typeof(string), typeof(int), typeof(bool)])]
    public async Task Test2()
    {
        _test2Start = TestContext.Current!.TestStart!.Value.DateTime;
        await Task.CompletedTask;
    }

    [Test]
    public async Task Test3()
    {
        await Assert.That(() => TestContext.Current!.GetTests(nameof(Test1))).ThrowsException().WithMessage("Cannot get unfinished tests - Did you mean to add a [DependsOn] attribute?");
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test2Start).IsAfterOrEqualTo(_test1Start.AddSeconds(4.9));
    }
}