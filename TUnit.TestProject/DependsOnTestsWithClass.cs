using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class DependsOnTestsOtherClass
{
    internal static DateTimeOffset Test1Start;

    [Test]
    public async Task Test1(CancellationToken cancellationToken)
    {
        Test1Start = TestContext.Current!.Execution.TestStart!.Value;
        var timeProvider = TestContext.Current!.TimeProvider;
        await timeProvider.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class DependsOnTestsWithClass
{
    private static DateTimeOffset _test2Start;

    [Test, DependsOn(typeof(DependsOnTestsOtherClass), nameof(DependsOnTestsOtherClass.Test1))]
    public async Task Test2()
    {
        _test2Start = TestContext.Current!.Execution.TestStart!.Value;
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test2Start).IsAfterOrEqualTo(DependsOnTestsOtherClass.Test1Start.AddSeconds(5));
    }
}
