using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Engine.Extensions;

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
        
        TestContext.Current!.ObjectBag.Add("Test1", "1");
    }
    
    [Test]
    public async Task Test2()
    {
        _test2Start = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        TestContext.Current!.ObjectBag.Add("Test2", "2");
    }
    
    [Test]
    [DependsOn(nameof(Test1))]
    [DependsOn(nameof(Test2))]
    public async Task Test3()
    {
        _test3Start = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(1));

        var test1 = TestContext.Current!.GetTests(nameof(Test1));
        var test2 = TestContext.Current!.GetTests(nameof(Test2));

        await Assert.That(test1).Has.Count().EqualTo(1);
        await Assert.That(test2).Has.Count().EqualTo(1);

        await Assert.That(test1[0].ObjectBag).Does.ContainKey("Test1");
        await Assert.That(test2[0].ObjectBag).Does.ContainKey("Test2");
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test3Start).Is.GreaterThanOrEqualTo(_test1Start.AddSeconds(1));
        await Assert.That(_test3Start).Is.GreaterThanOrEqualTo(_test2Start.AddSeconds(1));
    }
}