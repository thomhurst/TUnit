using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DependsOnTests3
{
    private static DateTimeOffset _test3Start;

    private static DateTimeOffset _test1End;
    private static DateTimeOffset _test2End;

    [Test]
    public async Task Test1(CancellationToken cancellationToken)
    {
        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        TestContext.Current!.StateBag.Items["Test1"] = "1";

        _test1End = timeProvider.GetUtcNow();
    }

    [Test]
    public async Task Test2(CancellationToken cancellationToken)
    {
        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        TestContext.Current!.StateBag.Items["Test2"] = "2";

        _test2End = timeProvider.GetUtcNow();
    }

    [Test]
    [DependsOn(nameof(Test1))]
    [DependsOn(nameof(Test2))]
    public async Task Test3(CancellationToken cancellationToken)
    {
        var timeProvider = TimeProviderContext.Current;
        _test3Start = timeProvider.GetUtcNow();

        await timeProvider.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        var test1 = TestContext.Current!.Dependencies.GetTests(nameof(Test1));
        var test2 = TestContext.Current.Dependencies.GetTests(nameof(Test2));

        await Assert.That(test1).HasCount().EqualTo(1);
        await Assert.That(test2).HasCount().EqualTo(1);

        await Assert.That(test1[0].StateBag.Items).ContainsKey("Test1");
        await Assert.That(test2[0].StateBag.Items).ContainsKey("Test2");
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test3Start).IsAfterOrEqualTo(_test1End);
        await Assert.That(_test3Start).IsAfterOrEqualTo(_test2End);
    }
}
