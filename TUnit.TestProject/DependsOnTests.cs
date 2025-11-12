using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class DependsOnTests
{
    private static DateTimeOffset _test1Start;
    private static DateTimeOffset _test2Start;

    [Test]
    public async Task Test1(CancellationToken cancellationToken)
    {
        var timeProvider = TimeProviderContext.Current;
        _test1Start = timeProvider.GetUtcNow();
        await timeProvider.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }

    [Test, DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        var timeProvider = TimeProviderContext.Current;
        _test2Start = timeProvider.GetUtcNow();
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(_test2Start).IsAfterOrEqualTo(_test1Start.AddSeconds(5));
    }
}
