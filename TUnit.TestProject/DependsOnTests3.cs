using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DependsOnTests3
{
    private static DateTime _test3Start;

    private static DateTime _test1End;
    private static DateTime _test2End;

    [Test]
    public async Task Test1()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));

        TestContext.Current!.ObjectBag["Test1"] = "1";

        _test1End = DateTime.UtcNow;
    }

    [Test]
    public async Task Test2()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));

        TestContext.Current!.ObjectBag["Test2"] = "2";

        _test2End = DateTime.UtcNow;
    }

    [Test]
    [DependsOn(nameof(Test1))]
    [DependsOn(nameof(Test2))]
    public async Task Test3()
    {
        _test3Start = DateTime.UtcNow;

        await Task.Delay(TimeSpan.FromSeconds(1));

        var test1 = TestContext.Current!.GetTests(nameof(Test1));
        var test2 = TestContext.Current.GetTests(nameof(Test2));

        await Assert.That(test1).HasCount().EqualTo(1);
        await Assert.That(test2).HasCount().EqualTo(1);

        await Assert.That(test1[0].ObjectBag).ContainsKey("Test1");
        await Assert.That(test2[0].ObjectBag).ContainsKey("Test2");
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test3Start).IsAfterOrEqualTo(_test1End);
        await Assert.That(_test3Start).IsAfterOrEqualTo(_test2End);
    }
}
