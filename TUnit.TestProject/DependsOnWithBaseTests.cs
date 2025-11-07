using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class DependsOnWithBaseTests : DependsOnBase
{
    private static DateTimeOffset _subTypeTestStart;

    [Test, DependsOn(nameof(BaseTest))]
    public async Task SubTypeTest()
    {
        _subTypeTestStart = TestContext.Current!.Execution.TestStart!.Value;
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(BaseTestStart).IsNotDefault();
        await Assert.That(_subTypeTestStart).IsAfterOrEqualTo(BaseTestStart.AddSeconds(5));
    }
}

public abstract class DependsOnBase
{
    protected static DateTimeOffset BaseTestStart { get; private set; }

    [Test]
    public async Task BaseTest(CancellationToken cancellationToken)
    {
        BaseTestStart = TestContext.Current!.Execution.TestStart!.Value;
        var timeProvider = TestContext.Current!.GetService<TimeProvider>();
        await timeProvider.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }
}
